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
using Newtonsoft.Json.Linq;

namespace HdtCollectionExporter.Tests
{
    [TestClass]
    public class StorageTests
    {
        private string _root;
        private SnapshotStore _store;
        private CollectionExportDocument _document;
        [TestInitialize] public async Task Setup()
        {
            _root = Path.Combine(Path.GetTempPath(), "ManacostStorageTests-" + Guid.NewGuid().ToString("N"));
            _store = new SnapshotStore(_root);
            var service = new CollectionExportService(new FakeProvider { Snapshot = ExporterTests.Snapshot() }, _store);
            _document = (await service.PrepareAsync(CancellationToken.None)).Document;
        }
        [TestCleanup] public void Cleanup() { if(Directory.Exists(_root)) Directory.Delete(_root, true); }
        [TestMethod] public void UnknownAccountRejected()
        {
            Assert.ThrowsException<InvalidOperationException>(() => SnapshotStore.AccountKey(null));
            Assert.ThrowsException<InvalidOperationException>(() => SnapshotStore.AccountKey(new UserProfileRecord()));
        }
        [TestMethod] public void AccountKeysPreserveUInt64()
        {
            var first = SnapshotStore.AccountKey(_document.User);
            var second = SnapshotStore.AccountKey(new UserProfileRecord { AccountHi = ulong.MaxValue, AccountLo = 9007199254740992 });
            Assert.AreNotEqual(first, second); Assert.AreEqual(64, first.Length);
            var entry = _store.Save(_document, true);
            var read = _store.Read(entry.Path).Document.User;
            Assert.AreEqual(ulong.MaxValue, read.AccountHi); Assert.AreEqual(9007199254740993UL, read.AccountLo);
        }
        [TestMethod] public void AccountsHaveIndependentBaselines()
        {
            _store.Save(_document, true); var firstUser = _document.User;
            _document.User = new UserProfileRecord { AccountHi = 5, AccountLo = 6 };
            Assert.IsNull(_store.Baseline(_document.User));
            _document.Dust = 987; _store.Save(_document, true);
            Assert.AreEqual(123, _store.Baseline(firstUser).Document.Dust);
            Assert.AreEqual(987, _store.Baseline(_document.User).Document.Dust);
        }
        [TestMethod] public void HistoryIsImmutableAndRebuildable()
        {
            var first = _store.Save(_document, true); var original = File.ReadAllText(first.Path);
            _document.Dust++; var second = _store.Save(_document, true);
            Assert.AreNotEqual(first.Path, second.Path); Assert.AreEqual(original, File.ReadAllText(first.Path));
            Assert.AreEqual(2, new SnapshotStore(_root).History(_document.User).Count);
            _store.Clear(_document.User); Assert.IsNull(_store.Baseline(_document.User)); Assert.AreEqual(2, _store.History(_document.User).Count);
        }
        [TestMethod] public void CorruptedHistoryMarked()
        {
            var entry = _store.Save(_document, true);
            var json = JObject.Parse(File.ReadAllText(entry.Path)); json["Document"]["dust"] = 10000;
            File.WriteAllText(entry.Path, json.ToString());
            Assert.ThrowsException<InvalidDataException>(() => _store.Read(entry.Path));
            Assert.IsFalse(_store.History(_document.User).Single().IsValid);
            Assert.ThrowsException<InvalidDataException>(() => _store.Baseline(_document.User));
        }
        [TestMethod] public void BackupRecoversPointer()
        {
            _store.Save(_document, true); _document.Dust++; _store.Save(_document, true);
            File.WriteAllText(_store.BaselinePath(_document.User), "broken");
            Assert.AreEqual(123, _store.Baseline(_document.User).Document.Dust);
        }
        [TestMethod] public void LegacyImportIsPreservedAndUntrusted()
        {
            Directory.CreateDirectory(_root); var source = Path.Combine(_root, "old-export.json");
            File.WriteAllText(source, SnapshotStore.Serialize(_document)); var original = File.ReadAllText(source);
            _store.MigrateLegacy(new[] { source }, _document.User); _store.MigrateLegacy(new[] { source }, _document.User);
            Assert.AreEqual(original, File.ReadAllText(source)); Assert.AreEqual(1, _store.History(_document.User).Count);
            Assert.IsFalse(_store.Baseline(_document.User).CompleteCounts);
        }
        [TestMethod] public void ImportedAccountMustMatch()
        {
            Directory.CreateDirectory(_root); var source = Path.Combine(_root, "import.json");
            File.WriteAllText(source, SnapshotStore.Serialize(_document));
            Assert.ThrowsException<InvalidOperationException>(() => _store.Import(source, new UserProfileRecord { AccountLo = 1 }));
            Assert.AreEqual(0, _store.History(_document.User).Count);
        }
        [TestMethod] public void DeltaImportRejected()
        {
            Directory.CreateDirectory(_root); var source = Path.Combine(_root, "delta.json"); File.WriteAllText(source, "{\"exportType\":\"changes\",\"cards\":[{}]}");
            Assert.ThrowsException<InvalidDataException>(() => _store.Import(source, _document.User));
            Assert.IsNull(_store.Baseline(_document.User));
        }
        [TestMethod] public void AtomicReplaceKeepsBackup()
        {
            var path = Path.Combine(_root, "file.txt"); var writer = new AtomicFile();
            writer.Write(path, "original", false); writer.Write(path, "new", true);
            Assert.AreEqual("new", File.ReadAllText(path)); Assert.AreEqual("original", File.ReadAllText(path + ".bak"));
            Assert.AreEqual(0, Directory.GetFiles(_root, "*.tmp").Length);
            Assert.ThrowsException<IOException>(() => writer.Write(path, "overwrite", false)); Assert.AreEqual("new", File.ReadAllText(path));
        }
        [TestMethod] public void BatchFailureReportsPartialFiles()
        {
            var first = Path.Combine(_root, "one.json"); var blocked = Path.Combine(_root, "two.csv");
            new AtomicFile().Write(blocked, "original", false);
            var error = Assert.ThrowsException<PartialExportException>(() => AtomicFile.WriteBatch(new Dictionary<string, string> { { first, "one" }, { blocked, "two" } }, CancellationToken.None));
            CollectionAssert.AreEqual(new[] { first }, error.Files.ToArray()); Assert.AreEqual("original", File.ReadAllText(blocked));
            Assert.AreEqual(0, Directory.GetFiles(_root, "*.tmp").Length); Assert.IsNull(_store.Baseline(_document.User));
        }
        [TestMethod] public void StageFailureExposesNoFinalFiles()
        {
            Directory.CreateDirectory(_root); var first = Path.Combine(_root, "one.json"); var blocker = Path.Combine(_root, "not-a-folder"); File.WriteAllText(blocker, "blocking");
            Assert.ThrowsException<IOException>(() => AtomicFile.WriteBatch(new Dictionary<string, string> { { first, "one" }, { Path.Combine(blocker, "two.csv"), "two" } }, CancellationToken.None));
            Assert.IsFalse(File.Exists(first)); Assert.AreEqual(0, Directory.GetFiles(_root, "*.tmp").Length);
        }
        [TestMethod] public void PrunePreservesActiveBaselineAndCorruptFiles()
        {
            var first = _store.Save(_document, true);
            _document.ExportedAt = "2099-01-01T00:00:00+00:00"; _store.Save(_document, true);
            var corrupt = Path.Combine(Path.GetDirectoryName(first.Path), "corrupt.json"); File.WriteAllText(corrupt, "broken");
            _store.Prune(_document.User, 1);
            Assert.IsTrue(File.Exists(first.Path)); Assert.IsTrue(File.Exists(corrupt));
            Assert.AreEqual(2, _store.History(_document.User).Count(h => h.IsValid));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => _store.Prune(_document.User, 0));
        }
        [TestMethod] public void HistorySelectionChangesOnlyPointer()
        {
            var first = _store.Save(_document, true); _document.Dust = 700; _store.Save(_document, true);
            _store.UseAsBaseline(first.Path, _document.User);
            Assert.AreEqual(123, _store.Baseline(_document.User).Document.Dust);
            Assert.AreEqual(2, _store.History(_document.User).Count);
            Assert.ThrowsException<InvalidOperationException>(() => _store.UseAsBaseline(first.Path, new UserProfileRecord { AccountLo = 10 }));
        }
        [TestMethod] public void SettingsRoundTripAndBackupRecovery()
        {
            var settings = new PluginSettings { OutputFolder = "C:\\Снимки", Language = "ru", IncludeMetadata = false, FormatIndex = 1 };
            settings.Save(_root); settings.Language = "en"; settings.Save(_root);
            Assert.AreEqual("en", PluginSettings.Load(_root).Language);
            File.WriteAllText(Path.Combine(_root, "settings.xml"), "corrupt");
            var recovered = PluginSettings.Load(_root);
            Assert.AreEqual("ru", recovered.Language); Assert.AreEqual("C:\\Снимки", recovered.OutputFolder); Assert.IsFalse(recovered.IncludeMetadata);
        }
        [TestMethod] public void RussianSettingsMigrateWithoutDeletion()
        {
            var ru = Path.Combine(_root, "legacy"); var target = Path.Combine(_root, "current");
            new PluginSettings { OutputFolder = "C:\\Legacy" }.Save(ru); var original = File.ReadAllText(Path.Combine(ru, "settings.xml"));
            PluginSettings.Migrate(target, ru); Assert.AreEqual("ru", PluginSettings.Load(target).Language);
            var settings = PluginSettings.Load(target); settings.OutputFolder = "C:\\New"; settings.Save(target);
            PluginSettings.Migrate(target, ru); Assert.AreEqual("C:\\New", PluginSettings.Load(target).OutputFolder);
            Assert.AreEqual(original, File.ReadAllText(Path.Combine(ru, "settings.xml")));
        }
    }
}
