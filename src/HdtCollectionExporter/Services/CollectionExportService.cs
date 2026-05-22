using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HdtCollectionExporter.Models;
using Newtonsoft.Json;

namespace HdtCollectionExporter.Services
{
    public class CollectionExportService
    {
        private const string ExportSource = "Hearthstone Deck Tracker plugin by Manacost";
        private const int ExportVersion = 2;
        private readonly ICollectionProvider _collectionProvider;
        private readonly string _baselineSnapshotPath;

        public CollectionExportService(ICollectionProvider collectionProvider)
            : this(collectionProvider, null)
        {
        }

        public CollectionExportService(ICollectionProvider collectionProvider, string baselineSnapshotPath)
        {
            if(collectionProvider == null)
                throw new ArgumentNullException("collectionProvider");
            _collectionProvider = collectionProvider;
            _baselineSnapshotPath = baselineSnapshotPath;
        }

        public async Task<ExportResult> ExportAsync(ExportFormat format, ExportOptions options)
        {
            if(options == null)
                throw new ArgumentNullException("options");
            if(string.IsNullOrWhiteSpace(options.OutputFolder))
                throw new InvalidOperationException("Output folder is empty.");

            var outputFolder = PrepareOutputFolder(options.OutputFolder);
            var snapshot = await GetSnapshotAsync(options);
            var exportedAt = DateTimeOffset.Now;
            var document = ToDocument(snapshot, exportedAt);
            var baseName = "hearthstone-collection-" + exportedAt.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var result = new ExportResult
            {
                ExportedAt = exportedAt,
                CardCount = document.Cards.Count
            };

            if((format & ExportFormat.Json) == ExportFormat.Json)
            {
                var path = Path.Combine(outputFolder, baseName + ".json");
                await Task.Run(delegate { WriteJson(path, document); });
                result.Files.Add(path);
            }

            if((format & ExportFormat.Csv) == ExportFormat.Csv)
            {
                var path = Path.Combine(outputFolder, baseName + ".csv");
                await Task.Run(delegate { WriteCsv(path, document.Cards); });
                result.Files.Add(path);
            }

            SaveBaselineDocument(document);
            return result;
        }

        public async Task<ExportResult> ExportChangesAsync(ExportOptions options)
        {
            if(options == null)
                throw new ArgumentNullException("options");
            if(string.IsNullOrWhiteSpace(options.OutputFolder))
                throw new InvalidOperationException("Output folder is empty.");

            var outputFolder = PrepareOutputFolder(options.OutputFolder);
            var previousDocument = LoadBaselineDocument(outputFolder);
            if(previousDocument == null)
            {
                throw new NoPreviousExportException(
                    "No previous collection export was found. Run a full export once before exporting changes.");
            }

            var snapshot = await GetSnapshotAsync(options);
            var exportedAt = DateTimeOffset.Now;
            var currentDocument = ToDocument(snapshot, exportedAt);
            var deltaDocument = BuildDeltaDocument(previousDocument, currentDocument, exportedAt);
            var baseName = "hearthstone-collection-changes-" + exportedAt.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var result = new ExportResult
            {
                ExportedAt = exportedAt,
                CardCount = currentDocument.Cards.Count,
                ChangeCount = deltaDocument.Summary.TotalChanges
            };

            var jsonPath = Path.Combine(outputFolder, baseName + ".json");
            var csvPath = Path.Combine(outputFolder, baseName + ".csv");
            await Task.Run(delegate
            {
                WriteDeltaJson(jsonPath, deltaDocument);
                WriteDeltaCsv(csvPath, deltaDocument.Cards);
            });
            result.Files.Add(jsonPath);
            result.Files.Add(csvPath);

            SaveBaselineDocument(currentDocument);
            return result;
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
                Cards = snapshot.Cards
                    .Select(ToJsonRecord)
                    .OrderBy(card => card.DbfId)
                    .ThenBy(card => card.CardId)
                    .ToList()
            };
        }

        private static void WriteJson(string path, CollectionExportDocument document)
        {
            var json = JsonConvert.SerializeObject(document, Formatting.Indented);
            File.WriteAllText(path, json, new UTF8Encoding(false));
        }

        private static void WriteDeltaJson(string path, CollectionDeltaExportDocument document)
        {
            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            var json = JsonConvert.SerializeObject(document, Formatting.Indented, settings);
            File.WriteAllText(path, json, new UTF8Encoding(false));
        }

        private void SaveBaselineDocument(CollectionExportDocument document)
        {
            if(string.IsNullOrWhiteSpace(_baselineSnapshotPath))
                return;

            var directory = Path.GetDirectoryName(_baselineSnapshotPath);
            if(!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            WriteJson(_baselineSnapshotPath, document);
        }

        private CollectionExportDocument LoadBaselineDocument(string outputFolder)
        {
            var document = TryReadDocument(_baselineSnapshotPath);
            if(document != null)
                return document;

            return LoadLatestFullExportDocument(outputFolder);
        }

        private static CollectionExportDocument LoadLatestFullExportDocument(string outputFolder)
        {
            if(string.IsNullOrWhiteSpace(outputFolder) || !Directory.Exists(outputFolder))
                return null;

            var files = Directory.GetFiles(outputFolder, "hearthstone-collection-*.json")
                .Where(path => Path.GetFileName(path).IndexOf("-changes-", StringComparison.OrdinalIgnoreCase) < 0)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToList();

            foreach(var file in files)
            {
                var document = TryReadDocument(file);
                if(document != null)
                    return document;
            }

            return null;
        }

        private static CollectionExportDocument TryReadDocument(string path)
        {
            if(string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            try
            {
                var document = JsonConvert.DeserializeObject<CollectionExportDocument>(File.ReadAllText(path));
                if(document == null || document.Cards == null)
                    return null;
                document.CardBacks = document.CardBacks ?? new List<int>();
                document.FavoriteHeroes = document.FavoriteHeroes ?? new List<FavoriteHeroRecord>();
                document.PlayerRecords = document.PlayerRecords ?? new List<PlayerRecordGroup>();
                return document;
            }
            catch
            {
                return null;
            }
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
            var dustChange = previous.Dust == current.Dust ? null : BuildNumericChange(previous.Dust, current.Dust);
            var favoriteCardBackChange = previous.FavoriteCardBack == current.FavoriteCardBack
                ? null
                : BuildNumericChange(previous.FavoriteCardBack, current.FavoriteCardBack);
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
            return string.Equals(previous.BattleTag ?? string.Empty, current.BattleTag ?? string.Empty, StringComparison.Ordinal) &&
                   previous.AccountHi == current.AccountHi &&
                   previous.AccountLo == current.AccountLo;
        }

        private static void WriteCsv(string path, IList<CollectionCardRecordJson> cards)
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
                    EscapeCsv(card.PremiumTotal.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(card.OwnedTotal.ToString(CultureInfo.InvariantCulture))
                }));
            }

            File.WriteAllText(path, string.Join(Environment.NewLine, lines), new UTF8Encoding(false));
        }

        private static void WriteDeltaCsv(string path, IList<CollectionCardDeltaRecord> cardChanges)
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
                    EscapeCsv(change.Delta.PremiumTotal.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(change.Delta.OwnedTotal.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(GetCurrentNormal(change.Previous).ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(GetCurrentPremiumTotal(change.Previous).ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(GetCurrentOwnedTotal(change.Previous).ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(GetCurrentNormal(change.Current).ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(GetCurrentPremiumTotal(change.Current).ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(GetCurrentOwnedTotal(change.Current).ToString(CultureInfo.InvariantCulture))
                }));
            }

            File.WriteAllText(path, string.Join(Environment.NewLine, lines), new UTF8Encoding(false));
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
