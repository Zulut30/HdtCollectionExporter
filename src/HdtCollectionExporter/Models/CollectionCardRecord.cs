namespace HdtCollectionExporter.Models
{
    public class CollectionCardRecord
    {
        public string CardId { get; set; }

        public int DbfId { get; set; }

        public string Name { get; set; }

        public string Set { get; set; }

        public string Rarity { get; set; }

        public string Class { get; set; }

        public int Normal { get; set; }

        public int Golden { get; set; }

        public int Diamond { get; set; }

        public int Signature { get; set; }

        public int TrialNormal { get; set; }

        public int TrialGolden { get; set; }

        public int TrialDiamond { get; set; }

        public int TrialSignature { get; set; }

        public int PremiumTotal
        {
            get { return Golden + Diamond + Signature; }
        }

        public int OwnedTotal
        {
            get { return Normal + PremiumTotal; }
        }
    }
}
