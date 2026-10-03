using System;
using System.Collections.Generic;
using System.Linq;
using HdtCollectionExporter.Services;

namespace HdtCollectionExporter.Models
{
    public sealed class CollectionPreview
    {
        public CollectionExportDocument Document { get; internal set; }
        public CollectionDeltaExportDocument Changes { get; internal set; }
        public DateTimeOffset ReadAt { get; internal set; }
        public string BaselineChecksum { get; internal set; }
        public string BaselineIssue { get; internal set; }
        public IList<CatalogCard> Catalog { get; internal set; }
        public int OwnedCards { get { return Document.Cards.Count(c => c.OwnedTotal > 0); } }
        public int Copies { get { return Document.Cards.Sum(c => c.OwnedTotal); } }
        public int Premium { get { return Document.Cards.Sum(c => c.PremiumTotal); } }
        public int Trial { get { return Document.Cards.Sum(c => c.TrialNormal + c.TrialGolden + c.TrialDiamond + c.TrialSignature); } }
        public IList<CollectionGroup> Sets { get { return Group(c => c.Set); } }
        public IList<CollectionGroup> Rarities { get { return Group(c => c.Rarity); } }
        private IList<CollectionGroup> Group(Func<CollectionCardRecordJson, string> key)
        {
            return Document.Cards.Where(c => c.OwnedTotal > 0 && !ExcludedSets.Contains(c.Set ?? "")).GroupBy(c => key(c) ?? "")
                .Select(g => {
                    var catalog = (Catalog ?? new List<CatalogCard>()).Where(c => !ExcludedSets.Contains(c.Set ?? "") && key(new CollectionCardRecordJson { Set = c.Set, Rarity = c.Rarity }) == g.Key).Select(c => c.CardId).Distinct().ToList();
                    return new CollectionGroup { Name = g.Key, Cards = g.Count(), Copies = g.Sum(c => c.OwnedTotal), Total = catalog.Count,
                        CatalogOwned = g.Select(c => c.CardId).Distinct().Count(id => catalog.Contains(id)) };
                })
                .OrderByDescending(g => g.Cards).ThenBy(g => g.Name, StringComparer.Ordinal).ToList();
        }
        private static readonly HashSet<string> ExcludedSets = new HashSet<string>(new[] { "CORE", "VANILLA", "LEGACY", "LETTUCE", "EVENT", "HERO_SKINS", "PLACEHOLDER_202204" }, StringComparer.OrdinalIgnoreCase);
    }
    public sealed class CollectionGroup
    {
        public string Name { get; set; }
        public int Cards { get; set; }
        public int Copies { get; set; }
        public int Total { get; set; }
        public int CatalogOwned { get; set; }
        public override string ToString() { return Name + "   ·   " + (Total > 0 ? CatalogOwned + " / " + Total + "  (" + ((double)CatalogOwned / Total).ToString("P0") + ")" : Cards.ToString("N0")) + "   ·   " + Copies.ToString("N0"); }
    }
    public sealed class CatalogCard
    {
        public string CardId { get; set; }
        public string Set { get; set; }
        public string Rarity { get; set; }
    }
}
