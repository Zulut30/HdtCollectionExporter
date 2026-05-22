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
        private const string ExportSource = "Hearthstone Deck Tracker plugin";
        private const int ExportVersion = 2;
        private readonly ICollectionProvider _collectionProvider;

        public CollectionExportService(ICollectionProvider collectionProvider)
        {
            if(collectionProvider == null)
                throw new ArgumentNullException("collectionProvider");
            _collectionProvider = collectionProvider;
        }

        public async Task<ExportResult> ExportAsync(ExportFormat format, ExportOptions options)
        {
            if(options == null)
                throw new ArgumentNullException("options");
            if(string.IsNullOrWhiteSpace(options.OutputFolder))
                throw new InvalidOperationException("Output folder is empty.");

            var outputFolder = Path.GetFullPath(Environment.ExpandEnvironmentVariables(options.OutputFolder));
            Directory.CreateDirectory(outputFolder);

            var snapshot = await _collectionProvider.GetCollectionAsync(options);
            if(snapshot == null || snapshot.Cards == null || snapshot.Cards.Count == 0)
                throw new CollectionUnavailableException("Collection data is empty.");

            var exportedAt = DateTimeOffset.Now;
            var baseName = "hearthstone-collection-" + exportedAt.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var result = new ExportResult
            {
                ExportedAt = exportedAt,
                CardCount = snapshot.Cards.Count
            };

            if((format & ExportFormat.Json) == ExportFormat.Json)
            {
                var path = Path.Combine(outputFolder, baseName + ".json");
                await Task.Run(delegate { WriteJson(path, snapshot, exportedAt); });
                result.Files.Add(path);
            }

            if((format & ExportFormat.Csv) == ExportFormat.Csv)
            {
                var path = Path.Combine(outputFolder, baseName + ".csv");
                await Task.Run(delegate { WriteCsv(path, snapshot.Cards); });
                result.Files.Add(path);
            }

            return result;
        }

        private static void WriteJson(string path, CollectionSnapshot snapshot, DateTimeOffset exportedAt)
        {
            var document = new CollectionExportDocument
            {
                ExportedAt = exportedAt.ToString("o", CultureInfo.InvariantCulture),
                Source = ExportSource,
                Version = ExportVersion,
                User = snapshot.User,
                Dust = snapshot.Dust,
                CardBacks = snapshot.CardBacks,
                FavoriteCardBack = snapshot.FavoriteCardBack,
                FavoriteHeroes = snapshot.FavoriteHeroes,
                PlayerRecords = snapshot.PlayerRecords,
                Cards = snapshot.Cards.Select(ToJsonRecord).ToList()
            };

            var json = JsonConvert.SerializeObject(document, Formatting.Indented);
            File.WriteAllText(path, json, new UTF8Encoding(false));
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

        private static void WriteCsv(string path, IReadOnlyList<CollectionCardRecord> cards)
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

        private static string EscapeCsv(string value)
        {
            if(value == null)
                return string.Empty;
            if(value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
                return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
