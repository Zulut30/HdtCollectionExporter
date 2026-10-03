using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HdtCollectionExporter.Models;
using HdtCollectionExporter.Services;
using HdtCollectionExporter.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HdtCollectionExporter.Tests
{
    [TestClass]
    public class ExporterTests
    {
        private string _root;
        private FakeProvider _provider;
        private CollectionExportService _service;
        private ExportOptions Options { get { return new ExportOptions { OutputFolder = Path.Combine(_root, "output"), IncludeCardNames = true, IncludeGoldenCount = true, IncludeMetadata = true }; } }
        [TestInitialize] public void Setup()
        {
            _root = Path.Combine(Path.GetTempPath(), "ManacostTests-" + Guid.NewGuid().ToString("N"));
            _provider = new FakeProvider { Snapshot = Snapshot() };
            _service = new CollectionExportService(_provider, new SnapshotStore(_root));
        }
        [TestCleanup] public void Cleanup() { if(Directory.Exists(_root)) Directory.Delete(_root, true); }
        internal static CollectionSnapshot Snapshot(ulong lo = 9007199254740993)
        {
            return new CollectionSnapshot { User = new UserProfileRecord { AccountHi = 18446744073709551615, AccountLo = lo, BattleTag = "Fixture#0001" }, Dust = 123,
                Cards = new List<CollectionCardRecord> { new CollectionCardRecord { CardId = "SYNTH_01", DbfId = 1, Name = "A, \"quoted\"\ncard", Set = "SYNTH", Rarity = "RARE", Class = "MAGE", Normal = 2, Golden = 1, Diamond = 1, Signature = 1, TrialNormal = 3, TrialGolden = 2, TrialDiamond = 1, TrialSignature = 1 } } };
        }
        private async Task<CollectionPreview> Prepare() { return await _service.PrepareAsync(CancellationToken.None); }

        [TestMethod] public async Task PremiumAndTrialCountsRemainCanonical()
        {
            var preview = await Prepare();
            Assert.AreEqual(5, preview.Document.Cards[0].OwnedTotal);
            Assert.AreEqual(3, preview.Document.Cards[0].PremiumTotal);
            Assert.AreEqual(7, preview.Trial);
            Assert.IsTrue(_provider.LastOptions.IncludeGoldenCount);
            Assert.IsTrue(_provider.LastOptions.IncludeCardNames);
            Assert.IsTrue(_provider.LastOptions.IncludeMetadata);
        }
        [TestMethod] public async Task PresentationOptionsDoNotCreateChanges()
        {
            var options = Options; options.IncludeGoldenCount = false; options.IncludeCardNames = false; options.IncludeMetadata = false;
            var result = await _service.ExportAsync(ExportFormat.Both, options);
            var json = JObject.Parse(File.ReadAllText(result.Files[0]));
            Assert.AreEqual(5, json["cards"][0].Value<int>("ownedTotal"));
            Assert.AreEqual(1, json["cards"][0].Value<int>("golden"));
            Assert.AreEqual("", json["cards"][0].Value<string>("name"));
            var baseline = _service.Store.Baseline(_provider.Snapshot.User);
            Assert.AreEqual("SYNTH", baseline.Document.Cards[0].Set);
            Assert.AreEqual(0, (await Prepare()).Changes.Summary.TotalChanges);
        }
        [TestMethod] public async Task MetadataAndBattleTagDoNotCreateChanges()
        {
            await _service.ExportAsync(ExportFormat.Json, Options);
            _provider.Snapshot.Cards[0].Name = "New translation";
            _provider.Snapshot.User.BattleTag = "Renamed#0002";
            Assert.AreEqual(0, (await Prepare()).Changes.Summary.TotalChanges);
        }
        [TestMethod] public async Task FirstDeltaCreatesBaselineWithoutFiles()
        {
            var result = await _service.ExportChangesAsync(ExportFormat.Both, Options);
            Assert.IsTrue(result.BaselineCreated);
            Assert.AreEqual(0, result.Files.Count);
            Assert.AreEqual(1, _service.Store.History(_provider.Snapshot.User).Count);
        }
        [TestMethod] public async Task PreviewAndExportUseSameSnapshot()
        {
            _provider.Snapshot.CardBacks = new List<int>();
            var preview = await Prepare();
            _provider.Snapshot.Cards[0].Normal = 12;
            _provider.Snapshot.User.BattleTag = "Later#0002";
            _provider.Snapshot.CardBacks.Add(77);
            var result = await _service.ExportPreparedAsync(preview, false, ExportFormat.Json, Options, CancellationToken.None);
            Assert.AreEqual(2, JObject.Parse(File.ReadAllText(result.Files[0]))["cards"][0].Value<int>("normal"));
            var document = JObject.Parse(File.ReadAllText(result.Files[0]));
            Assert.AreEqual("Fixture#0001", document["user"].Value<string>("battleTag"));
            Assert.AreEqual(0, ((JArray)document["cardBacks"]).Count);
            Assert.AreEqual(1, _provider.Reads);
        }
        [DataTestMethod]
        [DataRow("Normal")][DataRow("Golden")][DataRow("Diamond")][DataRow("Signature")]
        [DataRow("TrialNormal")][DataRow("TrialGolden")][DataRow("TrialDiamond")][DataRow("TrialSignature")]
        public async Task AllCountVariantsProduceDeltas(string property)
        {
            await _service.ExportAsync(ExportFormat.Json, Options);
            var card = _provider.Snapshot.Cards[0]; var member = card.GetType().GetProperty(property);
            member.SetValue(card, (int)member.GetValue(card) + 1);
            var delta = (await Prepare()).Changes;
            Assert.AreEqual(1, delta.Summary.CardChanges);
            Assert.AreEqual("changed", delta.Cards[0].ChangeType);
            Assert.AreEqual(1, (int)delta.Cards[0].Delta.GetType().GetProperty(property).GetValue(delta.Cards[0].Delta));
        }
        [TestMethod] public async Task AddedAndRemovedCardsAreCompared()
        {
            await _service.ExportAsync(ExportFormat.Json, Options);
            _provider.Snapshot.Cards = new List<CollectionCardRecord> { new CollectionCardRecord { CardId = "NEW", DbfId = 3, Normal = 1 } };
            var delta = (await Prepare()).Changes;
            Assert.AreEqual(1, delta.Summary.CardsAdded); Assert.AreEqual(1, delta.Summary.CardsRemoved);
            Assert.AreEqual(-5, delta.Cards.Single(c => c.ChangeType == "removed").Delta.OwnedTotal);
        }
        [TestMethod] public async Task NonCardChangesAreIncluded()
        {
            await _service.ExportAsync(ExportFormat.Json, Options);
            _provider.Snapshot.Dust += 40; _provider.Snapshot.CardBacks = new List<int> { 55 }; _provider.Snapshot.FavoriteCardBack = 55;
            var delta = (await Prepare()).Changes;
            Assert.AreEqual(40, delta.Dust.Delta); CollectionAssert.AreEqual(new[] { 55 }, delta.CardBacks.Added.ToArray());
            Assert.AreEqual(55, delta.FavoriteCardBack.Current); Assert.AreEqual(3, delta.Summary.TotalChanges);
        }
        [TestMethod] public async Task MissingProviderDataRejected()
        {
            _provider.Snapshot.Cards = new List<CollectionCardRecord>();
            await Assert.ThrowsExceptionAsync<CollectionUnavailableException>(Prepare);
            Assert.IsFalse(Directory.Exists(Path.Combine(_root, "accounts")));
        }
        [TestMethod] public async Task CancellationDoesNotWrite()
        {
            var preview = await Prepare(); var cancel = new CancellationTokenSource(); cancel.Cancel();
            await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => _service.ExportPreparedAsync(preview, false, ExportFormat.Json, Options, cancel.Token));
            Assert.AreEqual(0, _service.Store.History(preview.Document.User).Count);
            Assert.IsFalse(Directory.Exists(Options.OutputFolder));
        }
        [TestMethod] public async Task CancelledReadCannotCommitLateResult()
        {
            var deferred = new TaskCompletionSource<CollectionSnapshot>(); _provider.Pending = deferred.Task;
            var cancellation = new CancellationTokenSource();
            var reading = _service.PrepareAsync(cancellation.Token); cancellation.Cancel();
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => reading);
            deferred.SetResult(Snapshot());
            Assert.AreEqual(0, _service.Store.History(Snapshot().User).Count);
        }
        [TestMethod] public async Task RapidExportsHaveUniqueNames()
        {
            var preview = await Prepare();
            var first = await _service.ExportPreparedAsync(preview, false, ExportFormat.Both, Options, CancellationToken.None);
            var second = await _service.ExportPreparedAsync(preview, false, ExportFormat.Both, Options, CancellationToken.None);
            Assert.AreEqual(4, first.Files.Concat(second.Files).Distinct().Count());
            Assert.AreEqual(4, Directory.GetFiles(Options.OutputFolder).Length);
        }
        [TestMethod] public async Task CsvEscapesAndRetainsHeader()
        {
            var result = await _service.ExportAsync(ExportFormat.Csv, Options); var csv = File.ReadAllText(result.Files.Single());
            StringAssert.StartsWith(csv, "cardId,dbfId,name,set,rarity,class,normal,golden,ownedTotal");
            StringAssert.Contains(csv, "\"A, \"\"quoted\"\"\ncard\"");
            StringAssert.Contains(csv, ",2,1,5");
        }
        [TestMethod] public async Task InvalidFormatsRejected()
        {
            var preview = await Prepare();
            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => _service.ExportPreparedAsync(preview, false, (ExportFormat)0, Options, CancellationToken.None));
            Assert.AreEqual(0, _service.Store.History(preview.Document.User).Count);
        }
        [TestMethod] public async Task BaselineFailureReportsExportedFiles()
        {
            var store = new SnapshotStore(_root, new FailingWriter { FailBaseline = true });
            _service = new CollectionExportService(_provider, store);
            var result = await _service.ExportAsync(ExportFormat.Both, Options);
            Assert.AreEqual("BaselineSaveFailed", result.Warning); Assert.AreEqual(2, result.Files.Count);
            Assert.IsTrue(result.Files.All(File.Exists)); Assert.IsNull(store.Baseline(_provider.Snapshot.User));
        }
        [TestMethod] public async Task BaselineChangedAfterPreviewRequiresRefresh()
        {
            await _service.ExportAsync(ExportFormat.Json, Options); var preview = await Prepare();
            _service.ClearBaseline();
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _service.ExportPreparedAsync(preview, true, ExportFormat.Json, Options, CancellationToken.None));
            Assert.AreEqual(1, Directory.GetFiles(Options.OutputFolder).Length);
        }
        [TestMethod] public async Task SummarySeparatesOwnedAndTrial()
        {
            _provider.Snapshot.Cards = _provider.Snapshot.Cards.Concat(new[] { new CollectionCardRecord { DbfId = 2, CardId = "TRIAL", TrialGolden = 4, Set = "TRIAL" } }).ToList();
            var preview = await Prepare();
            Assert.AreEqual(1, preview.OwnedCards); Assert.AreEqual(5, preview.Copies); Assert.AreEqual(3, preview.Premium); Assert.AreEqual(11, preview.Trial);
            Assert.AreEqual("SYNTH", preview.Sets.Single().Name); Assert.AreEqual(5, preview.Rarities.Single().Copies);
        }
        [TestMethod] public async Task CompletionUsesCatalogAndExcludesSpecialSets()
        {
            _provider.Snapshot.Catalog = new List<CatalogCard> {
                new CatalogCard { CardId = "SYNTH_01", Set = "SYNTH", Rarity = "RARE" },
                new CatalogCard { CardId = "MISSING", Set = "SYNTH", Rarity = "RARE" },
                new CatalogCard { CardId = "CORE", Set = "CORE", Rarity = "RARE" } };
            var group = (await Prepare()).Sets.Single();
            Assert.AreEqual(2, group.Total); Assert.AreEqual(1, group.CatalogOwned);
            Assert.AreEqual(2, (await Prepare()).Rarities.Single().Total);
        }
        [TestMethod] public async Task HistoryCompareRequiresSameAccountAndChronology()
        {
            var a = await Prepare(); a.Document.ExportedAt = "2026-01-01T00:00:00+00:00"; var first = _service.Store.Save(a.Document, true);
            var b = await Prepare(); b.Document.ExportedAt = "2026-01-02T00:00:00+00:00"; b.Document.Cards[0].Normal++; b.Document.Cards[0].OwnedTotal++;
            var second = _service.Store.Save(b.Document, true);
            Assert.AreEqual(1, _service.CompareHistory(first.Path, second.Path).Changes.Summary.CardChanges);
            Assert.ThrowsException<InvalidOperationException>(() => _service.CompareHistory(second.Path, first.Path));
            _provider.Snapshot = Snapshot(42); var other = _service.Store.Save((await Prepare()).Document, true);
            Assert.ThrowsException<InvalidOperationException>(() => _service.CompareHistory(first.Path, other.Path));
            var before = _service.Store.Baseline(b.Document.User).Checksum;
            var files = await _service.ExportHistoryAsync(first.Path, second.Path, Options.OutputFolder, CancellationToken.None);
            Assert.AreEqual(2, files.Count); Assert.AreEqual(before, _service.Store.Baseline(b.Document.User).Checksum);
        }
        [TestMethod] public async Task CorruptBaselineCanBeRepairedByFullExport()
        {
            await _service.ExportAsync(ExportFormat.Json, Options);
            File.WriteAllText(_service.Store.BaselinePath(_provider.Snapshot.User), "bad");
            var preview = await Prepare(); Assert.AreEqual("CorruptBaseline", preview.BaselineIssue);
            await _service.ExportPreparedAsync(preview, false, ExportFormat.Json, Options, CancellationToken.None);
            Assert.AreEqual(5, _service.Store.Baseline(_provider.Snapshot.User).Document.Cards[0].OwnedTotal);
        }
    }
    internal sealed class FakeProvider : ICollectionProvider
    {
        public CollectionSnapshot Snapshot; public Task<CollectionSnapshot> Pending; public int Reads; public ExportOptions LastOptions;
        public Task<CollectionSnapshot> GetCollectionAsync(ExportOptions options) { Reads++; LastOptions = options; return Pending ?? Task.FromResult(Snapshot); }
    }
    internal sealed class FailingWriter : IAtomicFile
    {
        public bool FailBaseline;
        public void Write(string path, string text, bool overwrite) { if(FailBaseline && Path.GetFileName(path) == "baseline.json") throw new IOException("Synthetic failure"); new AtomicFile().Write(path, text, overwrite); }
    }
}
