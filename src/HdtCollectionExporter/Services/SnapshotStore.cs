using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HdtCollectionExporter.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HdtCollectionExporter.Services
{
    public sealed class StoredSnapshot
    {
        public int SnapshotVersion { get; set; }
        public bool CompleteCounts { get; set; }
        public string Checksum { get; set; }
        public CollectionExportDocument Document { get; set; }
        public IList<CatalogCard> Catalog { get; set; }
    }

    public sealed class HistoryEntry
    {
        public string Path { get; set; }
        public string Date { get; set; }
        public int Cards { get; set; }
        public long Bytes { get; set; }
        public bool IsValid { get; set; }
        public bool CompleteCounts { get; set; }
        public override string ToString() { return Date + "   ·   " + Cards.ToString("N0") + (IsValid ? "" : "   ⚠"); }
    }

    public sealed class SnapshotStore
    {
        private readonly string _root;
        private readonly IAtomicFile _writer;
        public SnapshotStore(string root, IAtomicFile writer = null)
        {
            _root = Path.GetFullPath(root);
            _writer = writer ?? new AtomicFile();
        }

        public static string AccountKey(UserProfileRecord user)
        {
            if(user == null || (user.AccountHi == 0 && user.AccountLo == 0))
                throw new InvalidOperationException("Account identity is unavailable.");
            return Hash(user.AccountHi.ToString(CultureInfo.InvariantCulture) + ":" + user.AccountLo.ToString(CultureInfo.InvariantCulture));
        }

        private string AccountDir(UserProfileRecord user) { return Path.Combine(_root, "accounts", AccountKey(user)); }
        public string BaselinePath(UserProfileRecord user) { return Path.Combine(AccountDir(user), "baseline.json"); }

        public StoredSnapshot Baseline(UserProfileRecord user)
        {
            var pointer = BaselinePath(user);
            if(!File.Exists(pointer) && !File.Exists(pointer + ".bak")) return null;
            Exception failure = null;
            foreach(var candidate in new[] { pointer, pointer + ".bak" })
            {
                if(!File.Exists(candidate)) continue;
                try
                {
                    var name = JObject.Parse(File.ReadAllText(candidate)).Value<string>("snapshot");
                    if(string.IsNullOrEmpty(name) || Path.GetFileName(name) != name) throw new InvalidDataException("Invalid baseline pointer.");
                    var snapshot = Read(Path.Combine(AccountDir(user), "history", name));
                    RequireSameAccount(user, snapshot.Document.User);
                    return snapshot;
                }
                catch(Exception ex) { failure = ex; }
            }
            throw new InvalidDataException("The baseline and its backup cannot be read. Choose a valid snapshot or save the current collection as the baseline.", failure);
        }

        public HistoryEntry Save(CollectionExportDocument document, bool completeCounts, IList<CatalogCard> catalog = null)
        {
            AccountKey(document.User);
            var snapshot = new StoredSnapshot { SnapshotVersion = 1, CompleteCounts = completeCounts, Catalog = catalog, Document = document, Checksum = Hash(Serialize(document)) };
            var name = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N") + ".json";
            var path = Path.Combine(AccountDir(document.User), "history", name);
            _writer.Write(path, Serialize(snapshot), false);
            _writer.Write(BaselinePath(document.User), Serialize(new { snapshot = name }), true);
            return Entry(path, snapshot);
        }

        public void Clear(UserProfileRecord user)
        {
            // Keep immutable history; only the active pointer is removed.
            var path = BaselinePath(user);
            if(File.Exists(path)) File.Delete(path);
            if(File.Exists(path + ".bak")) File.Delete(path + ".bak");
        }

        public StoredSnapshot Read(string path)
        {
            var snapshot = JsonConvert.DeserializeObject<StoredSnapshot>(File.ReadAllText(path));
            if(snapshot == null || snapshot.SnapshotVersion != 1 || snapshot.Document == null || snapshot.Document.Cards == null ||
               !string.Equals(snapshot.Checksum, Hash(Serialize(snapshot.Document)), StringComparison.Ordinal))
                throw new InvalidDataException("Snapshot checksum or version is invalid.");
            AccountKey(snapshot.Document.User);
            return snapshot;
        }

        public void UseAsBaseline(string path, UserProfileRecord user)
        {
            var expected = Path.Combine(AccountDir(user), "history");
            if(!string.Equals(Path.GetDirectoryName(Path.GetFullPath(path)), expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Select a snapshot from this account's history.");
            var snapshot = Read(path);
            RequireSameAccount(user, snapshot.Document.User);
            _writer.Write(BaselinePath(user), Serialize(new { snapshot = Path.GetFileName(path) }), true);
        }

        public IList<HistoryEntry> History(UserProfileRecord user)
        {
            var dir = Path.Combine(AccountDir(user), "history");
            if(!Directory.Exists(dir)) return new List<HistoryEntry>();
            // The directory is the authoritative index; deleting an index cannot lose history.
            return Directory.GetFiles(dir, "*.json").Select(path =>
            {
                try { var snapshot = Read(path); RequireSameAccount(user, snapshot.Document.User); return Entry(path, snapshot); }
                catch { return new HistoryEntry { Path = path, Date = Path.GetFileNameWithoutExtension(path), Bytes = new FileInfo(path).Length, IsValid = false }; }
            }).OrderByDescending(entry => entry.Date, StringComparer.Ordinal).ToList();
        }

        public HistoryEntry Import(string path, UserProfileRecord currentUser)
        {
            var json = JObject.Parse(File.ReadAllText(path));
            if(string.Equals(json.Value<string>("exportType"), "changes", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("A changes export cannot be used as a full baseline.");
            var document = json.ToObject<CollectionExportDocument>();
            if(document == null || document.Cards == null || document.Cards.Count == 0 || document.Version < 1 || document.Version > 3)
                throw new InvalidDataException("Invalid full collection export.");
            RequireSameAccount(currentUser, document.User);
            // Older exports do not record whether premium fields were filtered.
            // Keep their information, but never interpret unknown counts as zero.
            return Save(document, false);
        }

        public void MigrateLegacy(IEnumerable<string> candidates, UserProfileRecord user)
        {
            if(Baseline(user) != null) return;
            foreach(var path in candidates ?? new string[0])
            {
                if(!File.Exists(path)) continue;
                try { Import(path, user); return; }
                catch(JsonException) { }
                catch(InvalidDataException) { }
                catch(InvalidOperationException) { }
            }
        }

        public static void RequireSameAccount(UserProfileRecord first, UserProfileRecord second)
        {
            if(AccountKey(first) != AccountKey(second)) throw new InvalidOperationException("Snapshots belong to different accounts.");
        }
        public void Prune(UserProfileRecord user, int keep)
        {
            if(keep < 1) throw new ArgumentOutOfRangeException("keep");
            Baseline(user); // Refuse pruning when the baseline cannot be recovered.
            var protectedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var pointer in new[] { BaselinePath(user), BaselinePath(user) + ".bak" })
            {
                if(!File.Exists(pointer)) continue;
                try { protectedNames.Add(JObject.Parse(File.ReadAllText(pointer)).Value<string>("snapshot")); }
                catch(JsonException) { }
            }
            foreach(var entry in History(user).Skip(keep))
            {
                if(!entry.IsValid) continue; // Preserve damaged data for manual recovery.
                if(protectedNames.Contains(Path.GetFileName(entry.Path))) continue;
                File.Delete(entry.Path);
            }
        }
        private static HistoryEntry Entry(string path, StoredSnapshot snapshot)
        {
            return new HistoryEntry { Path = path, Date = snapshot.Document.ExportedAt, Cards = snapshot.Document.Cards.Count,
                Bytes = new FileInfo(path).Length, IsValid = true, CompleteCounts = snapshot.CompleteCounts };
        }
        public static string Serialize(object value) { return JsonConvert.SerializeObject(value, Formatting.Indented); }
        private static string Hash(string text)
        {
            using(var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
        }
    }
}
