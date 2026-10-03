using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HdtCollectionExporter.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HdtCollectionExporter.Services
{
    public class CollectionExportService
    {
        private const string ExportSource = "Hearthstone Deck Tracker plugin by Manacost";
        private const int ExportVersion = 3;
        private readonly ICollectionProvider _collectionProvider;
        private readonly IList<string> _legacyPaths;
        private readonly SemaphoreSlim _operation = new SemaphoreSlim(1, 1);
        private UserProfileRecord _currentUser;
        public SnapshotStore Store { get; private set; }

        public CollectionExportService(ICollectionProvider provider)
            : this(provider, (string)null, null) { }
        public CollectionExportService(ICollectionProvider provider, string baselinePath)
            : this(provider, baselinePath, null) { }
        public CollectionExportService(ICollectionProvider provider, string baselinePath, IEnumerable<string> candidates)
        {
            if(provider == null) throw new ArgumentNullException("provider");
            _collectionProvider = provider;
            _legacyPaths = BuildBaselineCandidatePaths(baselinePath, candidates);
            Store = new SnapshotStore(baselinePath == null
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HearthstoneDeckTracker", "HdtCollectionExporter")
                : Path.GetDirectoryName(Path.GetFullPath(baselinePath)));
        }
        public CollectionExportService(ICollectionProvider provider, SnapshotStore store)
        {
            if(provider == null) throw new ArgumentNullException("provider");
            _collectionProvider = provider;
            Store = store ?? throw new ArgumentNullException("store");
            _legacyPaths = new List<string>();
        }

        public async Task<CollectionPreview> PrepareAsync(CancellationToken token)
        {
            await _operation.WaitAsync(token);
            try
            {
                var read = GetSnapshotAsync(new ExportOptions { IncludeCardNames = true, IncludeGoldenCount = true, IncludeMetadata = true });
                var cancelled = new TaskCompletionSource<bool>();
                using(token.Register(() => cancelled.TrySetCanceled()))
                {
                    if(await Task.WhenAny(read, cancelled.Task) != read)
                    {
                        // Observe a late HDT fault after cancellation; never commit its result.
                        _ = read.ContinueWith(t => { var ignored = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                        token.ThrowIfCancellationRequested();
                    }
                    var snapshot = await read;
                    token.ThrowIfCancellationRequested();
                    var now = DateTimeOffset.Now;
                    var document = ToDocument(snapshot, now);
                    SnapshotStore.AccountKey(document.User);
                    _currentUser = document.User;
                    var preview = new CollectionPreview { Document = document, ReadAt = now, Catalog = snapshot.Catalog };
                    try
                    {
                        Store.MigrateLegacy(_legacyPaths, document.User);
                        var baseline = Store.Baseline(document.User);
                        if(baseline != null && baseline.CompleteCounts)
                        {
                            preview.Changes = Compare(baseline.Document, document);
                            preview.BaselineChecksum = baseline.Checksum;
                        }
                        else if(baseline != null) preview.BaselineIssue = "LegacyCountsUnknown";
                    }
                    catch(InvalidDataException) { preview.BaselineIssue = "CorruptBaseline"; }
                    return preview;
                }
            }
            finally { _operation.Release(); }
        }

        public async Task<ExportResult> ExportAsync(ExportFormat format, ExportOptions options)
        { return await ExportPreparedAsync(await PrepareAsync(CancellationToken.None), false, format, options, CancellationToken.None); }
        public async Task<ExportResult> ExportChangesAsync(ExportFormat format, ExportOptions options)
        { return await ExportPreparedAsync(await PrepareAsync(CancellationToken.None), true, format, options, CancellationToken.None); }

        public async Task<ExportResult> ExportPreparedAsync(CollectionPreview preview, bool changes, ExportFormat format, ExportOptions options, CancellationToken token)
        {
            if(preview == null) throw new ArgumentNullException("preview");
            if(options == null) throw new ArgumentNullException("options");
            if(format != ExportFormat.Json && format != ExportFormat.Csv && format != ExportFormat.Both) throw new ArgumentOutOfRangeException("format");
            if(string.IsNullOrWhiteSpace(options.OutputFolder)) throw new InvalidOperationException("Output folder is empty.");
            await _operation.WaitAsync(token);
            try
            {
                token.ThrowIfCancellationRequested();
                SnapshotStore.AccountKey(preview.Document.User);
                if(_currentUser != null) SnapshotStore.RequireSameAccount(_currentUser, preview.Document.User);
                StoredSnapshot baseline = null;
                try { baseline = Store.Baseline(preview.Document.User); }
                catch(InvalidDataException) { if(changes) throw; }
                if(changes && preview.BaselineIssue == "CorruptBaseline") throw new InvalidDataException("Repair the baseline before exporting changes.");
                if(changes && ((baseline == null || !baseline.CompleteCounts ? null : baseline.Checksum) != preview.BaselineChecksum))
                    throw new InvalidOperationException("Baseline changed. Refresh the preview.");
                var result = new ExportResult { ExportedAt = preview.ReadAt, CardCount = preview.Document.Cards.Count,
                    ChangeCount = preview.Changes == null ? 0 : preview.Changes.Summary.TotalChanges, BaselinePath = Store.BaselinePath(preview.Document.User) };
                if(changes && preview.Changes == null)
                {
                    Store.Save(preview.Document, true, preview.Catalog);
                    result.BaselineCreated = true;
                    return result;
                }
                var folder = PrepareOutputFolder(options.OutputFolder);
                var name = "hearthstone-collection-" + (changes ? "changes-" : "") + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                var files = new Dictionary<string, string>();
                // Public schema v3 remains stable. Count fields always contain the actual
                // inventory; display options only hide names/metadata and the CSV golden column.
                var projected = JsonConvert.DeserializeObject<CollectionExportDocument>(SnapshotStore.Serialize(preview.Document));
                foreach(var card in projected.Cards)
                {
                    if(!options.IncludeCardNames) card.Name = "";
                    if(!options.IncludeMetadata) { card.Set = ""; card.Rarity = ""; card.Class = ""; }
                }
                if((format & ExportFormat.Json) != 0) files.Add(Path.Combine(folder, name + ".json"),
                    changes ? JsonConvert.SerializeObject(preview.Changes, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }) : SnapshotStore.Serialize(projected));
                if((format & ExportFormat.Csv) != 0) files.Add(Path.Combine(folder, name + ".csv"), changes ? DeltaCsv(preview.Changes.Cards) : FullCsv(projected.Cards, options.IncludeGoldenCount));
                var completed = await Task.Run(() => AtomicFile.WriteBatch(files, token));
                foreach(var path in completed) result.Files.Add(path);
                try { Store.Save(preview.Document, true, preview.Catalog); }
                catch(Exception ex) { result.Warning = "BaselineSaveFailed"; result.WarningDetails = ex.Message; }
                return result;
            }
            finally { _operation.Release(); }
        }

        public async Task<BaselineStatus> SaveCurrentAsBaselineAsync(ExportOptions options)
        {
            var preview = await PrepareAsync(CancellationToken.None);
            SavePreparedBaseline(preview);
            return GetBaselineStatus();
        }
        public void SavePreparedBaseline(CollectionPreview preview)
        {
            _operation.Wait();
            try { Store.Save(preview.Document, true, preview.Catalog); }
            finally { _operation.Release(); }
        }
        public BaselineStatus ImportBaselineFile(string path)
        {
            _operation.Wait();
            try { Store.Import(path, _currentUser); }
            finally { _operation.Release(); }
            return GetBaselineStatus();
        }
        public void ClearBaseline()
        {
            _operation.Wait();
            try { Store.Clear(_currentUser); }
            finally { _operation.Release(); }
        }
        public BaselineStatus GetBaselineStatus()
        {
            if(_currentUser == null) return new BaselineStatus { Exists = false };
            var baseline = Store.Baseline(_currentUser);
            return new BaselineStatus { Exists = baseline != null, Path = Store.BaselinePath(_currentUser),
                CardCount = baseline == null ? 0 : baseline.Document.Cards.Count, ExportedAt = baseline == null ? null : baseline.Document.ExportedAt };
        }
        public static CollectionDeltaExportDocument Compare(CollectionExportDocument previous, CollectionExportDocument current)
        {
            SnapshotStore.RequireSameAccount(previous.User, current.User);
            return BuildDeltaDocument(previous, current, DateTimeOffset.Now);
        }
        public CollectionPreview CompareHistory(string first, string second)
        {
            var a = Store.Read(first); var b = Store.Read(second);
            SnapshotStore.RequireSameAccount(a.Document.User, b.Document.User);
            if(!a.CompleteCounts || !b.CompleteCounts) throw new InvalidDataException("Historical counts are incomplete.");
            if(DateTimeOffset.Parse(a.Document.ExportedAt, CultureInfo.InvariantCulture) >= DateTimeOffset.Parse(b.Document.ExportedAt, CultureInfo.InvariantCulture))
                throw new InvalidOperationException("Choose an earlier snapshot and a later snapshot.");
            return new CollectionPreview { Document = b.Document, Changes = Compare(a.Document, b.Document), ReadAt = DateTimeOffset.Parse(b.Document.ExportedAt, CultureInfo.InvariantCulture) };
        }
        public async Task<IList<string>> ExportHistoryAsync(string first, string second, string folder, CancellationToken token)
        {
            var preview = CompareHistory(first, second);
            var name = "hearthstone-collection-changes-history-" + Guid.NewGuid().ToString("N");
            var output = PrepareOutputFolder(folder);
            return await Task.Run(() => AtomicFile.WriteBatch(new Dictionary<string, string> {
                { Path.Combine(output, name + ".json"), SnapshotStore.Serialize(preview.Changes) },
                { Path.Combine(output, name + ".csv"), DeltaCsv(preview.Changes.Cards) } }, token));
        }

        private async Task<CollectionSnapshot> GetSnapshotAsync(ExportOptions options)
        {
            var snapshot = await _collectionProvider.GetCollectionAsync(options);
            if(snapshot == null || snapshot.Cards == null || snapshot.Cards.Count == 0)
                throw new CollectionUnavailableException("Collection data is empty.");
            return snapshot;
        }

        private static string PrepareOutputFolder(string outputFolder)
        {
            var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(outputFolder));
            Directory.CreateDirectory(fullPath);
            return fullPath;
        }

        private static CollectionExportDocument ToDocument(CollectionSnapshot snapshot, DateTimeOffset exportedAt)
        {
            return new CollectionExportDocument
            {
                ExportedAt = exportedAt.ToString("o", CultureInfo.InvariantCulture),
                Source = ExportSource,
                Version = ExportVersion,
                User = snapshot.User,
                Dust = snapshot.Dust,
                CardBacks = snapshot.CardBacks ?? new List<int>(),
                FavoriteCardBack = snapshot.FavoriteCardBack,
                FavoriteHeroes = snapshot.FavoriteHeroes ?? new List<FavoriteHeroRecord>(),
                PlayerRecords = snapshot.PlayerRecords ?? new List<PlayerRecordGroup>(),
                ClassStats = snapshot.ClassStats ?? new List<ClassStatRecord>(),
                FavoriteClass = snapshot.FavoriteClass,
                BestClassByWins = snapshot.BestClassByWins,
                Cards = snapshot.Cards
                    .Select(ToJsonRecord)
                    .OrderBy(card => card.DbfId)
                    .ThenBy(card => card.CardId)
                    .ToList()
            };
        }

        private static IList<string> BuildBaselineCandidatePaths(
            string primaryPath,
            IEnumerable<string> additionalPaths)
        {
            var paths = new List<string>();
            if(!string.IsNullOrWhiteSpace(primaryPath))
                paths.Add(primaryPath);
            if(additionalPaths != null)
            {
                foreach(var path in additionalPaths)
                {
                    if(!string.IsNullOrWhiteSpace(path))
                        paths.Add(path);
                }
            }

            return paths
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static CollectionDeltaExportDocument BuildDeltaDocument(
            CollectionExportDocument previous,
            CollectionExportDocument current,
            DateTimeOffset exportedAt)
        {
            var cardChanges = BuildCardChanges(previous.Cards, current.Cards);
            var cardBackChanges = BuildIntListDelta(previous.CardBacks, current.CardBacks);
            var favoriteHeroChanges = BuildFavoriteHeroesDelta(previous.FavoriteHeroes, current.FavoriteHeroes);
            var playerRecordChanges = BuildPlayerRecordDeltas(previous.PlayerRecords, current.PlayerRecords);
            var classStatChanges = BuildClassStatDeltas(previous.ClassStats, current.ClassStats);
            var dustChange = previous.Dust == current.Dust ? null : BuildNumericChange(previous.Dust, current.Dust);
            var favoriteCardBackChange = previous.FavoriteCardBack == current.FavoriteCardBack
                ? null
                : BuildNumericChange(previous.FavoriteCardBack, current.FavoriteCardBack);
            var favoriteClassChange = AreFavoriteClassesEqual(previous.FavoriteClass, current.FavoriteClass)
                ? null
                : new ValueChange<FavoriteClassRecord> { Previous = previous.FavoriteClass, Current = current.FavoriteClass };
            var bestClassByWinsChange = AreFavoriteClassesEqual(previous.BestClassByWins, current.BestClassByWins)
                ? null
                : new ValueChange<FavoriteClassRecord> { Previous = previous.BestClassByWins, Current = current.BestClassByWins };
            var userChange = AreUsersEqual(previous.User, current.User)
                ? null
                : new ValueChange<UserProfileRecord> { Previous = previous.User, Current = current.User };

            var summary = new CollectionDeltaSummary
            {
                CardChanges = cardChanges.Count,
                CardsAdded = cardChanges.Count(change => change.ChangeType == "added"),
                CardsRemoved = cardChanges.Count(change => change.ChangeType == "removed"),
                CardsChanged = cardChanges.Count(change => change.ChangeType == "changed"),
                DustChanged = dustChange != null,
                CardBacksAdded = cardBackChanges.Added.Count,
                CardBacksRemoved = cardBackChanges.Removed.Count,
                FavoriteCardBackChanged = favoriteCardBackChange != null,
                FavoriteHeroesAdded = favoriteHeroChanges.Added.Count,
                FavoriteHeroesRemoved = favoriteHeroChanges.Removed.Count,
                PlayerRecordChanges = playerRecordChanges.Count,
                ClassStatChanges = classStatChanges.Count,
                FavoriteClassChanged = favoriteClassChange != null,
                BestClassByWinsChanged = bestClassByWinsChange != null,
                UserChanged = userChange != null
            };
            summary.TotalChanges = summary.CardChanges +
                                   (summary.DustChanged ? 1 : 0) +
                                   summary.CardBacksAdded +
                                   summary.CardBacksRemoved +
                                   (summary.FavoriteCardBackChanged ? 1 : 0) +
                                   summary.FavoriteHeroesAdded +
                                   summary.FavoriteHeroesRemoved +
                                   summary.PlayerRecordChanges +
                                   summary.ClassStatChanges +
                                   (summary.FavoriteClassChanged ? 1 : 0) +
                                   (summary.BestClassByWinsChanged ? 1 : 0) +
                                   (summary.UserChanged ? 1 : 0);

            return new CollectionDeltaExportDocument
            {
                ExportedAt = exportedAt.ToString("o", CultureInfo.InvariantCulture),
                Source = ExportSource,
                Version = ExportVersion,
                ExportType = "changes",
                BaselineExportedAt = previous.ExportedAt,
                CurrentExportedAt = current.ExportedAt,
                Summary = summary,
                User = userChange,
                Dust = dustChange,
                CardBacks = cardBackChanges,
                FavoriteCardBack = favoriteCardBackChange,
                FavoriteHeroes = favoriteHeroChanges,
                PlayerRecords = playerRecordChanges,
                ClassStats = classStatChanges,
                FavoriteClass = favoriteClassChange,
                BestClassByWins = bestClassByWinsChange,
                Cards = cardChanges
            };
        }

        private static CollectionCardRecordJson ToJsonRecord(CollectionCardRecord record)
        {
            return new CollectionCardRecordJson
            {
                CardId = record.CardId,
                DbfId = record.DbfId,
                Name = record.Name,
                Set = record.Set,
                Rarity = record.Rarity,
                Class = record.Class,
                Normal = record.Normal,
                Golden = record.Golden,
                Diamond = record.Diamond,
                Signature = record.Signature,
                PremiumTotal = record.PremiumTotal,
                TrialNormal = record.TrialNormal,
                TrialGolden = record.TrialGolden,
                TrialDiamond = record.TrialDiamond,
                TrialSignature = record.TrialSignature,
                OwnedTotal = record.OwnedTotal
            };
        }

        private static List<CollectionCardDeltaRecord> BuildCardChanges(
            IList<CollectionCardRecordJson> previousCards,
            IList<CollectionCardRecordJson> currentCards)
        {
            var previous = BuildCardLookup(previousCards);
            var current = BuildCardLookup(currentCards);
            var keys = new HashSet<string>(previous.Keys, StringComparer.OrdinalIgnoreCase);
            keys.UnionWith(current.Keys);

            var changes = new List<CollectionCardDeltaRecord>();
            foreach(var key in keys.OrderBy(x => x))
            {
                CollectionCardRecordJson previousCard;
                CollectionCardRecordJson currentCard;
                previous.TryGetValue(key, out previousCard);
                current.TryGetValue(key, out currentCard);

                if(previousCard != null && currentCard != null && AreCardCountsEqual(previousCard, currentCard))
                    continue;

                var identity = currentCard ?? previousCard;
                changes.Add(new CollectionCardDeltaRecord
                {
                    ChangeType = previousCard == null ? "added" : currentCard == null ? "removed" : "changed",
                    CardId = identity.CardId,
                    DbfId = identity.DbfId,
                    Name = identity.Name,
                    Set = identity.Set,
                    Rarity = identity.Rarity,
                    Class = identity.Class,
                    Previous = previousCard,
                    Current = currentCard,
                    Delta = BuildCardCountDelta(previousCard, currentCard)
                });
            }

            return changes
                .OrderBy(change => change.DbfId)
                .ThenBy(change => change.CardId)
                .ToList();
        }

        private static Dictionary<string, CollectionCardRecordJson> BuildCardLookup(IList<CollectionCardRecordJson> cards)
        {
            var result = new Dictionary<string, CollectionCardRecordJson>(StringComparer.OrdinalIgnoreCase);
            if(cards == null)
                return result;

            foreach(var card in cards)
            {
                if(card == null)
                    continue;
                result[GetCardKey(card)] = card;
            }

            return result;
        }

        private static string GetCardKey(CollectionCardRecordJson card)
        {
            if(card == null)
                return string.Empty;
            if(!string.IsNullOrWhiteSpace(card.CardId))
                return "card:" + card.CardId;
            return "dbf:" + card.DbfId.ToString(CultureInfo.InvariantCulture);
        }

        private static bool AreCardCountsEqual(CollectionCardRecordJson previous, CollectionCardRecordJson current)
        {
            return previous.Normal == current.Normal &&
                   previous.Golden == current.Golden &&
                   previous.Diamond == current.Diamond &&
                   previous.Signature == current.Signature &&
                   previous.PremiumTotal == current.PremiumTotal &&
                   previous.TrialNormal == current.TrialNormal &&
                   previous.TrialGolden == current.TrialGolden &&
                   previous.TrialDiamond == current.TrialDiamond &&
                   previous.TrialSignature == current.TrialSignature &&
                   previous.OwnedTotal == current.OwnedTotal;
        }

        private static CollectionCardCountDelta BuildCardCountDelta(
            CollectionCardRecordJson previous,
            CollectionCardRecordJson current)
        {
            return new CollectionCardCountDelta
            {
                Normal = GetCurrentNormal(current) - GetCurrentNormal(previous),
                Golden = GetCurrentGolden(current) - GetCurrentGolden(previous),
                Diamond = GetCurrentDiamond(current) - GetCurrentDiamond(previous),
                Signature = GetCurrentSignature(current) - GetCurrentSignature(previous),
                PremiumTotal = GetCurrentPremiumTotal(current) - GetCurrentPremiumTotal(previous),
                TrialNormal = GetCurrentTrialNormal(current) - GetCurrentTrialNormal(previous),
                TrialGolden = GetCurrentTrialGolden(current) - GetCurrentTrialGolden(previous),
                TrialDiamond = GetCurrentTrialDiamond(current) - GetCurrentTrialDiamond(previous),
                TrialSignature = GetCurrentTrialSignature(current) - GetCurrentTrialSignature(previous),
                OwnedTotal = GetCurrentOwnedTotal(current) - GetCurrentOwnedTotal(previous)
            };
        }

        private static int GetCurrentNormal(CollectionCardRecordJson card)
        {
            return card == null ? 0 : card.Normal;
        }

        private static int GetCurrentGolden(CollectionCardRecordJson card)
        {
            return card == null ? 0 : card.Golden;
        }

        private static int GetCurrentDiamond(CollectionCardRecordJson card)
        {
            return card == null ? 0 : card.Diamond;
        }

        private static int GetCurrentSignature(CollectionCardRecordJson card)
        {
            return card == null ? 0 : card.Signature;
        }

        private static int GetCurrentPremiumTotal(CollectionCardRecordJson card)
        {
            return card == null ? 0 : card.PremiumTotal;
        }

        private static int GetCurrentTrialNormal(CollectionCardRecordJson card)
        {
            return card == null ? 0 : card.TrialNormal;
        }

        private static int GetCurrentTrialGolden(CollectionCardRecordJson card)
        {
            return card == null ? 0 : card.TrialGolden;
        }

        private static int GetCurrentTrialDiamond(CollectionCardRecordJson card)
        {
            return card == null ? 0 : card.TrialDiamond;
        }

        private static int GetCurrentTrialSignature(CollectionCardRecordJson card)
        {
            return card == null ? 0 : card.TrialSignature;
        }

        private static int GetCurrentOwnedTotal(CollectionCardRecordJson card)
        {
            return card == null ? 0 : card.OwnedTotal;
        }

        private static IntListDelta BuildIntListDelta(IList<int> previous, IList<int> current)
        {
            var previousSet = new HashSet<int>(previous ?? new List<int>());
            var currentSet = new HashSet<int>(current ?? new List<int>());
            return new IntListDelta
            {
                Added = currentSet.Where(value => !previousSet.Contains(value)).OrderBy(value => value).ToList(),
                Removed = previousSet.Where(value => !currentSet.Contains(value)).OrderBy(value => value).ToList()
            };
        }

        private static FavoriteHeroesDelta BuildFavoriteHeroesDelta(
            IList<FavoriteHeroRecord> previousHeroes,
            IList<FavoriteHeroRecord> currentHeroes)
        {
            var previous = BuildFavoriteHeroLookup(previousHeroes);
            var current = BuildFavoriteHeroLookup(currentHeroes);
            return new FavoriteHeroesDelta
            {
                Added = current
                    .Where(pair => !previous.ContainsKey(pair.Key))
                    .Select(pair => pair.Value)
                    .OrderBy(hero => hero.HeroKey)
                    .ThenBy(hero => hero.DbfId)
                    .ToList(),
                Removed = previous
                    .Where(pair => !current.ContainsKey(pair.Key))
                    .Select(pair => pair.Value)
                    .OrderBy(hero => hero.HeroKey)
                    .ThenBy(hero => hero.DbfId)
                    .ToList()
            };
        }

        private static Dictionary<string, FavoriteHeroRecord> BuildFavoriteHeroLookup(IList<FavoriteHeroRecord> heroes)
        {
            var result = new Dictionary<string, FavoriteHeroRecord>(StringComparer.OrdinalIgnoreCase);
            if(heroes == null)
                return result;

            foreach(var hero in heroes)
            {
                if(hero == null)
                    continue;
                var key = hero.HeroKey.ToString(CultureInfo.InvariantCulture) + ":" +
                          hero.DbfId.ToString(CultureInfo.InvariantCulture) + ":" +
                          (hero.CardId ?? string.Empty);
                result[key] = hero;
            }

            return result;
        }

        private static List<PlayerRecordDelta> BuildPlayerRecordDeltas(
            IList<PlayerRecordGroup> previousGroups,
            IList<PlayerRecordGroup> currentGroups)
        {
            var previous = BuildPlayerRecordLookup(previousGroups);
            var current = BuildPlayerRecordLookup(currentGroups);
            var keys = new HashSet<string>(previous.Keys, StringComparer.OrdinalIgnoreCase);
            keys.UnionWith(current.Keys);

            var changes = new List<PlayerRecordDelta>();
            foreach(var key in keys.OrderBy(x => x))
            {
                PlayerRecordLookupEntry previousEntry;
                PlayerRecordLookupEntry currentEntry;
                previous.TryGetValue(key, out previousEntry);
                current.TryGetValue(key, out currentEntry);

                var previousRecord = previousEntry != null ? previousEntry.Record : null;
                var currentRecord = currentEntry != null ? currentEntry.Record : null;
                if(previousRecord != null && currentRecord != null && ArePlayerRecordsEqual(previousRecord, currentRecord))
                    continue;

                var identity = currentEntry ?? previousEntry;
                changes.Add(new PlayerRecordDelta
                {
                    Type = identity.Type,
                    Data = identity.Data,
                    Previous = previousRecord,
                    Current = currentRecord,
                    Delta = BuildPlayerRecordDelta(previousRecord, currentRecord, identity.Data)
                });
            }

            return changes
                .OrderBy(change => change.Type)
                .ThenBy(change => change.Data)
                .ToList();
        }

        private static List<ClassStatDelta> BuildClassStatDeltas(
            IList<ClassStatRecord> previousStats,
            IList<ClassStatRecord> currentStats)
        {
            var previous = BuildClassStatLookup(previousStats);
            var current = BuildClassStatLookup(currentStats);
            var keys = new HashSet<string>(previous.Keys, StringComparer.OrdinalIgnoreCase);
            keys.UnionWith(current.Keys);

            var changes = new List<ClassStatDelta>();
            foreach(var key in keys.OrderBy(x => x))
            {
                ClassStatRecord previousStat;
                ClassStatRecord currentStat;
                previous.TryGetValue(key, out previousStat);
                current.TryGetValue(key, out currentStat);

                if(previousStat != null && currentStat != null && AreClassStatsEqual(previousStat, currentStat))
                    continue;

                var identity = currentStat ?? previousStat;
                changes.Add(new ClassStatDelta
                {
                    Class = identity.Class,
                    Previous = previousStat,
                    Current = currentStat,
                    Delta = BuildClassStatDelta(previousStat, currentStat)
                });
            }

            return changes
                .OrderByDescending(change => Math.Abs(change.Delta.Games))
                .ThenBy(change => change.Class)
                .ToList();
        }

        private static Dictionary<string, ClassStatRecord> BuildClassStatLookup(IList<ClassStatRecord> stats)
        {
            var result = new Dictionary<string, ClassStatRecord>(StringComparer.OrdinalIgnoreCase);
            if(stats == null)
                return result;

            foreach(var stat in stats)
            {
                if(stat == null || string.IsNullOrWhiteSpace(stat.Class))
                    continue;
                result[stat.Class] = stat;
            }

            return result;
        }

        private static bool AreClassStatsEqual(ClassStatRecord previous, ClassStatRecord current)
        {
            return previous.Wins == current.Wins &&
                   previous.Losses == current.Losses &&
                   previous.Ties == current.Ties &&
                   previous.Games == current.Games;
        }

        private static ClassStatCountDelta BuildClassStatDelta(ClassStatRecord previous, ClassStatRecord current)
        {
            return new ClassStatCountDelta
            {
                Wins = GetClassWins(current) - GetClassWins(previous),
                Losses = GetClassLosses(current) - GetClassLosses(previous),
                Ties = GetClassTies(current) - GetClassTies(previous),
                Games = GetClassGames(current) - GetClassGames(previous)
            };
        }

        private static int GetClassWins(ClassStatRecord stat)
        {
            return stat == null ? 0 : stat.Wins;
        }

        private static int GetClassLosses(ClassStatRecord stat)
        {
            return stat == null ? 0 : stat.Losses;
        }

        private static int GetClassTies(ClassStatRecord stat)
        {
            return stat == null ? 0 : stat.Ties;
        }

        private static int GetClassGames(ClassStatRecord stat)
        {
            return stat == null ? 0 : stat.Games;
        }

        private static Dictionary<string, PlayerRecordLookupEntry> BuildPlayerRecordLookup(IList<PlayerRecordGroup> groups)
        {
            var result = new Dictionary<string, PlayerRecordLookupEntry>(StringComparer.OrdinalIgnoreCase);
            if(groups == null)
                return result;

            foreach(var group in groups)
            {
                if(group == null || group.Records == null)
                    continue;
                foreach(var record in group.Records)
                {
                    if(record == null)
                        continue;
                    var key = group.Type.ToString(CultureInfo.InvariantCulture) + ":" +
                              record.Data.ToString(CultureInfo.InvariantCulture);
                    result[key] = new PlayerRecordLookupEntry
                    {
                        Type = group.Type,
                        Data = record.Data,
                        Record = record
                    };
                }
            }

            return result;
        }

        private static bool ArePlayerRecordsEqual(PlayerRecordEntry previous, PlayerRecordEntry current)
        {
            return previous.Wins == current.Wins &&
                   previous.Losses == current.Losses &&
                   previous.Ties == current.Ties;
        }

        private static PlayerRecordEntry BuildPlayerRecordDelta(
            PlayerRecordEntry previous,
            PlayerRecordEntry current,
            int data)
        {
            return new PlayerRecordEntry
            {
                Data = data,
                Wins = GetWins(current) - GetWins(previous),
                Losses = GetLosses(current) - GetLosses(previous),
                Ties = GetTies(current) - GetTies(previous)
            };
        }

        private static int GetWins(PlayerRecordEntry record)
        {
            return record == null ? 0 : record.Wins;
        }

        private static int GetLosses(PlayerRecordEntry record)
        {
            return record == null ? 0 : record.Losses;
        }

        private static int GetTies(PlayerRecordEntry record)
        {
            return record == null ? 0 : record.Ties;
        }

        private static NumericChange BuildNumericChange(int previous, int current)
        {
            return new NumericChange
            {
                Previous = previous,
                Current = current,
                Delta = current - previous
            };
        }

        private static bool AreUsersEqual(UserProfileRecord previous, UserProfileRecord current)
        {
            if(previous == null && current == null)
                return true;
            if(previous == null || current == null)
                return false;
            return previous.AccountHi == current.AccountHi &&
                   previous.AccountLo == current.AccountLo;
        }

        private static bool AreFavoriteClassesEqual(FavoriteClassRecord previous, FavoriteClassRecord current)
        {
            if(previous == null && current == null)
                return true;
            if(previous == null || current == null)
                return false;
            return string.Equals(previous.Class ?? string.Empty, current.Class ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(previous.Reason ?? string.Empty, current.Reason ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                   previous.Wins == current.Wins &&
                   previous.Losses == current.Losses &&
                   previous.Ties == current.Ties &&
                   previous.Games == current.Games;
        }

        public static string FullCsv(IList<CollectionCardRecordJson> cards, bool includeGolden = true)
        {
            var lines = new List<string> { "cardId,dbfId,name,set,rarity,class,normal,golden,ownedTotal" };
            foreach(var card in cards)
            {
                lines.Add(string.Join(",", new[]
                {
                    EscapeCsv(card.CardId),
                    EscapeCsv(card.DbfId.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(card.Name),
                    EscapeCsv(card.Set),
                    EscapeCsv(card.Rarity),
                    EscapeCsv(card.Class),
                    EscapeCsv(card.Normal.ToString(CultureInfo.InvariantCulture)),
                    includeGolden ? EscapeCsv(card.Golden.ToString(CultureInfo.InvariantCulture)) : "",
                    EscapeCsv(card.OwnedTotal.ToString(CultureInfo.InvariantCulture))
                }));
            }

            return string.Join(Environment.NewLine, lines);
        }

        public static string DeltaCsv(IList<CollectionCardDeltaRecord> cardChanges)
        {
            var lines = new List<string>
            {
                "changeType,cardId,dbfId,name,set,rarity,class,normalDelta,goldenDelta,ownedTotalDelta,previousNormal,previousGolden,previousOwnedTotal,currentNormal,currentGolden,currentOwnedTotal"
            };

            foreach(var change in cardChanges)
            {
                lines.Add(string.Join(",", new[]
                {
                    EscapeCsv(change.ChangeType),
                    EscapeCsv(change.CardId),
                    EscapeCsv(change.DbfId.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(change.Name),
                    EscapeCsv(change.Set),
                    EscapeCsv(change.Rarity),
                    EscapeCsv(change.Class),
                    EscapeCsv(change.Delta.Normal.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(change.Delta.Golden.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(change.Delta.OwnedTotal.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(GetCurrentNormal(change.Previous).ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(GetCurrentGolden(change.Previous).ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(GetCurrentOwnedTotal(change.Previous).ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(GetCurrentNormal(change.Current).ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(GetCurrentGolden(change.Current).ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(GetCurrentOwnedTotal(change.Current).ToString(CultureInfo.InvariantCulture))
                }));
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string EscapeCsv(string value)
        {
            if(value == null)
                return string.Empty;
            if(value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
                return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private class PlayerRecordLookupEntry
        {
            public int Type { get; set; }

            public int Data { get; set; }

            public PlayerRecordEntry Record { get; set; }
        }
    }
}
