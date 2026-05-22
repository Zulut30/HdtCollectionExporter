using System.Collections.Generic;
using Newtonsoft.Json;

namespace HdtCollectionExporter.Models
{
    public class CollectionDeltaExportDocument
    {
        [JsonProperty("exportedAt")]
        public string ExportedAt { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("exportType")]
        public string ExportType { get; set; }

        [JsonProperty("baselineExportedAt")]
        public string BaselineExportedAt { get; set; }

        [JsonProperty("currentExportedAt")]
        public string CurrentExportedAt { get; set; }

        [JsonProperty("summary")]
        public CollectionDeltaSummary Summary { get; set; }

        [JsonProperty("user")]
        public ValueChange<UserProfileRecord> User { get; set; }

        [JsonProperty("dust")]
        public NumericChange Dust { get; set; }

        [JsonProperty("cardBacks")]
        public IntListDelta CardBacks { get; set; }

        [JsonProperty("favoriteCardBack")]
        public NumericChange FavoriteCardBack { get; set; }

        [JsonProperty("favoriteHeroes")]
        public FavoriteHeroesDelta FavoriteHeroes { get; set; }

        [JsonProperty("playerRecords")]
        public IList<PlayerRecordDelta> PlayerRecords { get; set; }

        [JsonProperty("classStats")]
        public IList<ClassStatDelta> ClassStats { get; set; }

        [JsonProperty("favoriteClass")]
        public ValueChange<FavoriteClassRecord> FavoriteClass { get; set; }

        [JsonProperty("bestClassByWins")]
        public ValueChange<FavoriteClassRecord> BestClassByWins { get; set; }

        [JsonProperty("cards")]
        public IList<CollectionCardDeltaRecord> Cards { get; set; }
    }

    public class CollectionDeltaSummary
    {
        [JsonProperty("totalChanges")]
        public int TotalChanges { get; set; }

        [JsonProperty("cardChanges")]
        public int CardChanges { get; set; }

        [JsonProperty("cardsAdded")]
        public int CardsAdded { get; set; }

        [JsonProperty("cardsRemoved")]
        public int CardsRemoved { get; set; }

        [JsonProperty("cardsChanged")]
        public int CardsChanged { get; set; }

        [JsonProperty("dustChanged")]
        public bool DustChanged { get; set; }

        [JsonProperty("cardBacksAdded")]
        public int CardBacksAdded { get; set; }

        [JsonProperty("cardBacksRemoved")]
        public int CardBacksRemoved { get; set; }

        [JsonProperty("favoriteCardBackChanged")]
        public bool FavoriteCardBackChanged { get; set; }

        [JsonProperty("favoriteHeroesAdded")]
        public int FavoriteHeroesAdded { get; set; }

        [JsonProperty("favoriteHeroesRemoved")]
        public int FavoriteHeroesRemoved { get; set; }

        [JsonProperty("playerRecordChanges")]
        public int PlayerRecordChanges { get; set; }

        [JsonProperty("classStatChanges")]
        public int ClassStatChanges { get; set; }

        [JsonProperty("favoriteClassChanged")]
        public bool FavoriteClassChanged { get; set; }

        [JsonProperty("bestClassByWinsChanged")]
        public bool BestClassByWinsChanged { get; set; }

        [JsonProperty("userChanged")]
        public bool UserChanged { get; set; }
    }

    public class ValueChange<T>
    {
        [JsonProperty("previous")]
        public T Previous { get; set; }

        [JsonProperty("current")]
        public T Current { get; set; }
    }

    public class NumericChange
    {
        [JsonProperty("previous")]
        public int Previous { get; set; }

        [JsonProperty("current")]
        public int Current { get; set; }

        [JsonProperty("delta")]
        public int Delta { get; set; }
    }

    public class IntListDelta
    {
        [JsonProperty("added")]
        public IList<int> Added { get; set; }

        [JsonProperty("removed")]
        public IList<int> Removed { get; set; }
    }

    public class FavoriteHeroesDelta
    {
        [JsonProperty("added")]
        public IList<FavoriteHeroRecord> Added { get; set; }

        [JsonProperty("removed")]
        public IList<FavoriteHeroRecord> Removed { get; set; }
    }

    public class PlayerRecordDelta
    {
        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("data")]
        public int Data { get; set; }

        [JsonProperty("previous")]
        public PlayerRecordEntry Previous { get; set; }

        [JsonProperty("current")]
        public PlayerRecordEntry Current { get; set; }

        [JsonProperty("delta")]
        public PlayerRecordEntry Delta { get; set; }
    }

    public class CollectionCardDeltaRecord
    {
        [JsonProperty("changeType")]
        public string ChangeType { get; set; }

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

        [JsonProperty("previous")]
        public CollectionCardRecordJson Previous { get; set; }

        [JsonProperty("current")]
        public CollectionCardRecordJson Current { get; set; }

        [JsonProperty("delta")]
        public CollectionCardCountDelta Delta { get; set; }
    }

    public class ClassStatDelta
    {
        [JsonProperty("class")]
        public string Class { get; set; }

        [JsonProperty("previous")]
        public ClassStatRecord Previous { get; set; }

        [JsonProperty("current")]
        public ClassStatRecord Current { get; set; }

        [JsonProperty("delta")]
        public ClassStatCountDelta Delta { get; set; }
    }

    public class ClassStatCountDelta
    {
        [JsonProperty("wins")]
        public int Wins { get; set; }

        [JsonProperty("losses")]
        public int Losses { get; set; }

        [JsonProperty("ties")]
        public int Ties { get; set; }

        [JsonProperty("games")]
        public int Games { get; set; }
    }

    public class CollectionCardCountDelta
    {
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
