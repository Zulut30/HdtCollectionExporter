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
}
