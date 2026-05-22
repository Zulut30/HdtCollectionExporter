using System.Collections.Generic;
using Newtonsoft.Json;

namespace HdtCollectionExporter.Models
{
    public class CollectionSnapshot
    {
        public UserProfileRecord User { get; set; }

        public int Dust { get; set; }

        public IList<int> CardBacks { get; set; }

        public int FavoriteCardBack { get; set; }

        public IList<FavoriteHeroRecord> FavoriteHeroes { get; set; }

        public IList<PlayerRecordGroup> PlayerRecords { get; set; }

        public IList<ClassStatRecord> ClassStats { get; set; }

        public FavoriteClassRecord FavoriteClass { get; set; }

        public FavoriteClassRecord BestClassByWins { get; set; }

        public IReadOnlyList<CollectionCardRecord> Cards { get; set; }
    }

    public class UserProfileRecord
    {
        [JsonProperty("battleTag")]
        public string BattleTag { get; set; }

        [JsonProperty("accountHi")]
        public ulong AccountHi { get; set; }

        [JsonProperty("accountLo")]
        public ulong AccountLo { get; set; }
    }

    public class FavoriteHeroRecord
    {
        [JsonProperty("heroKey")]
        public int HeroKey { get; set; }

        [JsonProperty("dbfId")]
        public int DbfId { get; set; }

        [JsonProperty("cardId")]
        public string CardId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("class")]
        public string Class { get; set; }
    }

    public class PlayerRecordGroup
    {
        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("records")]
        public IList<PlayerRecordEntry> Records { get; set; }
    }

    public class PlayerRecordEntry
    {
        [JsonProperty("data")]
        public int Data { get; set; }

        [JsonProperty("wins")]
        public int Wins { get; set; }

        [JsonProperty("losses")]
        public int Losses { get; set; }

        [JsonProperty("ties")]
        public int Ties { get; set; }
    }

    public class ClassStatRecord
    {
        [JsonProperty("class")]
        public string Class { get; set; }

        [JsonProperty("wins")]
        public int Wins { get; set; }

        [JsonProperty("losses")]
        public int Losses { get; set; }

        [JsonProperty("ties")]
        public int Ties { get; set; }

        [JsonProperty("games")]
        public int Games { get; set; }

        [JsonProperty("winrate")]
        public double Winrate { get; set; }

        [JsonProperty("recordTypes")]
        public IList<ClassRecordTypeStat> RecordTypes { get; set; }
    }

    public class ClassRecordTypeStat
    {
        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("wins")]
        public int Wins { get; set; }

        [JsonProperty("losses")]
        public int Losses { get; set; }

        [JsonProperty("ties")]
        public int Ties { get; set; }

        [JsonProperty("games")]
        public int Games { get; set; }

        [JsonProperty("heroDbfIds")]
        public IList<int> HeroDbfIds { get; set; }
    }

    public class FavoriteClassRecord
    {
        [JsonProperty("class")]
        public string Class { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("wins")]
        public int Wins { get; set; }

        [JsonProperty("losses")]
        public int Losses { get; set; }

        [JsonProperty("ties")]
        public int Ties { get; set; }

        [JsonProperty("games")]
        public int Games { get; set; }

        [JsonProperty("winrate")]
        public double Winrate { get; set; }
    }
}
