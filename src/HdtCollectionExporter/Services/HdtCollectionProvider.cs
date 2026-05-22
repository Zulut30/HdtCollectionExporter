using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HearthDb.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;
using HdtCollectionExporter.Models;

namespace HdtCollectionExporter.Services
{
    public class HdtCollectionProvider : ICollectionProvider
    {
        public async Task<CollectionSnapshot> GetCollectionAsync(ExportOptions options)
        {
            if(options == null)
                throw new ArgumentNullException("options");

            Collection collection;
            try
            {
                collection = await CollectionHelpers.Hearthstone.GetCollection();
            }
            catch(Exception ex)
            {
                throw new CollectionUnavailableException(
                    "HDT could not read the Hearthstone collection. Start Hearthstone, log in, and try again.",
                    ex);
            }

            if(collection == null || collection.Cards == null || collection.Cards.Count == 0)
            {
                throw new CollectionUnavailableException(
                    "No Hearthstone collection data is available yet. Start Hearthstone, log in, and wait for HDT to read the collection.");
            }

            var cards = new List<CollectionCardRecord>();
            foreach(var entry in collection.Cards)
            {
                var dbfId = entry.Key;
                var counts = entry.Value ?? new int[0];
                var card = Database.GetCardFromDbfId(dbfId, false);

                var normal = SafeCount(counts, 0);
                var golden = options.IncludeGoldenCount ? SafeCount(counts, 1) : 0;
                var diamond = options.IncludeGoldenCount ? SafeCount(counts, 2) : 0;
                var signature = options.IncludeGoldenCount ? SafeCount(counts, 3) : 0;

                cards.Add(new CollectionCardRecord
                {
                    CardId = card != null ? card.Id ?? string.Empty : string.Empty,
                    DbfId = dbfId,
                    Name = options.IncludeCardNames && card != null
                        ? FirstNonEmpty(card.Name, card.LocalizedName)
                        : string.Empty,
                    Set = options.IncludeMetadata && card != null && card.CardSet.HasValue
                        ? card.CardSet.Value.ToString()
                        : string.Empty,
                    Rarity = options.IncludeMetadata && card != null && card.Rarity != Rarity.INVALID
                        ? card.Rarity.ToString()
                        : string.Empty,
                    Class = options.IncludeMetadata && card != null && card.CardClass != CardClass.INVALID
                        ? card.CardClass.ToString()
                        : string.Empty,
                    Normal = normal,
                    Golden = golden,
                    Diamond = diamond,
                    Signature = signature,
                    TrialNormal = SafeCount(counts, 4),
                    TrialGolden = SafeCount(counts, 5),
                    TrialDiamond = SafeCount(counts, 6),
                    TrialSignature = SafeCount(counts, 7)
                });
            }

            var playerRecords = BuildPlayerRecords(collection.PlayerRecords);
            var classStats = BuildClassStats(playerRecords);

            return new CollectionSnapshot
            {
                User = new UserProfileRecord
                {
                    BattleTag = collection.BattleTag ?? string.Empty,
                    AccountHi = collection.AccountHi,
                    AccountLo = collection.AccountLo
                },
                Dust = collection.Dust,
                CardBacks = collection.CardBacks != null ? collection.CardBacks.ToList() : new List<int>(),
                FavoriteCardBack = collection.FavoriteCardBack,
                FavoriteHeroes = BuildFavoriteHeroes(collection.FavoriteHeroes),
                PlayerRecords = playerRecords,
                ClassStats = classStats,
                FavoriteClass = BuildFavoriteClass(classStats, "mostGames"),
                BestClassByWins = BuildFavoriteClass(classStats, "mostWins"),
                Cards = cards
                    .OrderBy(x => x.DbfId)
                    .ThenBy(x => x.CardId)
                    .ToList()
            };
        }

        private static List<FavoriteHeroRecord> BuildFavoriteHeroes(SortedDictionary<int, int> favoriteHeroes)
        {
            var result = new List<FavoriteHeroRecord>();
            if(favoriteHeroes == null)
                return result;

            foreach(var entry in favoriteHeroes)
            {
                var card = Database.GetCardFromDbfId(entry.Value, false);
                result.Add(new FavoriteHeroRecord
                {
                    HeroKey = entry.Key,
                    DbfId = entry.Value,
                    CardId = card != null ? card.Id ?? string.Empty : string.Empty,
                    Name = card != null ? FirstNonEmpty(card.Name, card.LocalizedName) : string.Empty,
                    Class = card != null && card.CardClass != CardClass.INVALID ? card.CardClass.ToString() : string.Empty
                });
            }

            return result;
        }

        private static List<PlayerRecordGroup> BuildPlayerRecords(SortedDictionary<int, SortedDictionary<int, int[]>> playerRecords)
        {
            var result = new List<PlayerRecordGroup>();
            if(playerRecords == null)
                return result;

            foreach(var typeGroup in playerRecords)
            {
                var group = new PlayerRecordGroup
                {
                    Type = typeGroup.Key,
                    Records = new List<PlayerRecordEntry>()
                };

                if(typeGroup.Value != null)
                {
                    foreach(var record in typeGroup.Value)
                    {
                        var counts = record.Value ?? new int[0];
                        group.Records.Add(new PlayerRecordEntry
                        {
                            Data = record.Key,
                            Wins = SafeCount(counts, 0),
                            Losses = SafeCount(counts, 1),
                            Ties = SafeCount(counts, 2)
                        });
                    }
                }

                result.Add(group);
            }

            return result;
        }

        private static List<ClassStatRecord> BuildClassStats(IList<PlayerRecordGroup> playerRecords)
        {
            var byClass = new Dictionary<string, ClassStatBuilder>(StringComparer.OrdinalIgnoreCase);
            if(playerRecords == null)
                return new List<ClassStatRecord>();

            foreach(var group in playerRecords)
            {
                if(group == null || group.Records == null)
                    continue;

                foreach(var record in group.Records)
                {
                    if(record == null || record.Data <= 0)
                        continue;

                    var className = GetClassFromHeroDbfId(record.Data);
                    if(string.IsNullOrWhiteSpace(className))
                        continue;

                    ClassStatBuilder builder;
                    if(!byClass.TryGetValue(className, out builder))
                    {
                        builder = new ClassStatBuilder { Class = className };
                        byClass[className] = builder;
                    }

                    builder.Add(group.Type, record);
                }
            }

            return byClass.Values
                .Select(x => x.ToRecord())
                .OrderByDescending(x => x.Games)
                .ThenByDescending(x => x.Wins)
                .ThenBy(x => x.Class)
                .ToList();
        }

        private static string GetClassFromHeroDbfId(int dbfId)
        {
            var card = Database.GetCardFromDbfId(dbfId, false);
            if(card == null || card.CardClass == CardClass.INVALID || card.CardClass == CardClass.NEUTRAL)
                return string.Empty;
            return card.CardClass.ToString();
        }

        private static FavoriteClassRecord BuildFavoriteClass(IList<ClassStatRecord> classStats, string reason)
        {
            if(classStats == null || classStats.Count == 0)
                return null;

            var stat = string.Equals(reason, "mostWins", StringComparison.OrdinalIgnoreCase)
                ? classStats
                    .OrderByDescending(x => x.Wins)
                    .ThenByDescending(x => x.Games)
                    .ThenBy(x => x.Class)
                    .FirstOrDefault()
                : classStats
                    .OrderByDescending(x => x.Games)
                    .ThenByDescending(x => x.Wins)
                    .ThenBy(x => x.Class)
                    .FirstOrDefault();

            if(stat == null)
                return null;

            return new FavoriteClassRecord
            {
                Class = stat.Class,
                Reason = reason,
                Wins = stat.Wins,
                Losses = stat.Losses,
                Ties = stat.Ties,
                Games = stat.Games,
                Winrate = stat.Winrate
            };
        }

        private static int SafeCount(int[] counts, int index)
        {
            if(counts == null || index < 0 || index >= counts.Length)
                return 0;
            return Math.Max(0, counts[index]);
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach(var value in values)
            {
                if(!string.IsNullOrWhiteSpace(value))
                    return value;
            }
            return string.Empty;
        }

        private class ClassStatBuilder
        {
            private readonly Dictionary<int, ClassRecordTypeBuilder> _recordTypes =
                new Dictionary<int, ClassRecordTypeBuilder>();

            public string Class { get; set; }

            public int Wins { get; private set; }

            public int Losses { get; private set; }

            public int Ties { get; private set; }

            public void Add(int type, PlayerRecordEntry record)
            {
                Wins += record.Wins;
                Losses += record.Losses;
                Ties += record.Ties;

                ClassRecordTypeBuilder typeBuilder;
                if(!_recordTypes.TryGetValue(type, out typeBuilder))
                {
                    typeBuilder = new ClassRecordTypeBuilder { Type = type };
                    _recordTypes[type] = typeBuilder;
                }

                typeBuilder.Add(record);
            }

            public ClassStatRecord ToRecord()
            {
                var games = Wins + Losses + Ties;
                return new ClassStatRecord
                {
                    Class = Class,
                    Wins = Wins,
                    Losses = Losses,
                    Ties = Ties,
                    Games = games,
                    Winrate = CalculateWinrate(Wins, games),
                    RecordTypes = _recordTypes.Values
                        .Select(x => x.ToRecord())
                        .OrderByDescending(x => x.Games)
                        .ThenBy(x => x.Type)
                        .ToList()
                };
            }
        }

        private class ClassRecordTypeBuilder
        {
            private readonly SortedSet<int> _heroDbfIds = new SortedSet<int>();

            public int Type { get; set; }

            public int Wins { get; private set; }

            public int Losses { get; private set; }

            public int Ties { get; private set; }

            public void Add(PlayerRecordEntry record)
            {
                Wins += record.Wins;
                Losses += record.Losses;
                Ties += record.Ties;
                _heroDbfIds.Add(record.Data);
            }

            public ClassRecordTypeStat ToRecord()
            {
                var games = Wins + Losses + Ties;
                return new ClassRecordTypeStat
                {
                    Type = Type,
                    Wins = Wins,
                    Losses = Losses,
                    Ties = Ties,
                    Games = games,
                    HeroDbfIds = _heroDbfIds.ToList()
                };
            }
        }

        private static double CalculateWinrate(int wins, int games)
        {
            if(games <= 0)
                return 0;
            return Math.Round((double)wins / games, 4);
        }
    }
}
