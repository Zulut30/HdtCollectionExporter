using System.Collections.Generic;
using Newtonsoft.Json;

namespace HdtCollectionExporter.Models
{
    public class CollectionExportDocument
    {
        [JsonProperty("exportedAt")]
        public string ExportedAt { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("user")]
        public UserProfileRecord User { get; set; }

        [JsonProperty("dust")]
        public int Dust { get; set; }

        [JsonProperty("cardBacks")]
        public IList<int> CardBacks { get; set; }

        [JsonProperty("favoriteCardBack")]
        public int FavoriteCardBack { get; set; }

        [JsonProperty("favoriteHeroes")]
        public IList<FavoriteHeroRecord> FavoriteHeroes { get; set; }

        [JsonProperty("playerRecords")]
        public IList<PlayerRecordGroup> PlayerRecords { get; set; }

        [JsonProperty("classStats")]
        public IList<ClassStatRecord> ClassStats { get; set; }

        [JsonProperty("favoriteClass")]
        public FavoriteClassRecord FavoriteClass { get; set; }

        [JsonProperty("bestClassByWins")]
        public FavoriteClassRecord BestClassByWins { get; set; }

        [JsonProperty("cards")]
        public IList<CollectionCardRecordJson> Cards { get; set; }
    }

    public class CollectionCardRecordJson
    {
        [JsonProperty("cardId")]
        public string CardId { get; set; }

        [JsonProperty("dbfId")]
        public int DbfId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("set")]
        public string Set { get; set; }

        [JsonProperty("rarity")]
        public string Rarity { get; set; }

        [JsonProperty("class")]
        public string Class { get; set; }

        [JsonProperty("normal")]
        public int Normal { get; set; }

        [JsonProperty("golden")]
        public int Golden { get; set; }

        [JsonProperty("diamond")]
        public int Diamond { get; set; }

        [JsonProperty("signature")]
        public int Signature { get; set; }

        [JsonProperty("premiumTotal")]
        public int PremiumTotal { get; set; }

        [JsonProperty("trialNormal")]
        public int TrialNormal { get; set; }

        [JsonProperty("trialGolden")]
        public int TrialGolden { get; set; }

        [JsonProperty("trialDiamond")]
        public int TrialDiamond { get; set; }

        [JsonProperty("trialSignature")]
        public int TrialSignature { get; set; }

        [JsonProperty("ownedTotal")]
        public int OwnedTotal { get; set; }
    }
}
