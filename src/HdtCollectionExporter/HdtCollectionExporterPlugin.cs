using System;
using System.IO;
using System.Windows.Controls;
using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Plugins;
using HdtCollectionExporter.Services;
using HdtCollectionExporter.UI;
using ExporterSettings = HdtCollectionExporter.Settings.PluginSettings;

namespace HdtCollectionExporter
{
    public class HdtCollectionExporterPlugin : IPlugin
    {
        private MenuItem _menuItem;
        private ExportWindow _window;
        private ExporterSettings _settings;
        private CollectionExportService _exportService;

        public string Name
        {
            get { return "Collection Exporter by Manacost"; }
        }

        public string Description
        {
            get { return "Exports your Hearthstone collection from HDT to local JSON and CSV files. Built by the Manacost team."; }
        }

        public string ButtonText
        {
            get { return "Open exporter"; }
        }

        public string Author
        {
            get { return "Manacost"; }
        }

        public Version Version
        {
            get { return typeof(HdtCollectionExporterPlugin).Assembly.GetName().Version; }
        }

        public MenuItem MenuItem
        {
            get { return _menuItem; }
        }

        internal static string PluginDataDir
        {
            get { return Path.Combine(Config.Instance.DataDir, "HdtCollectionExporter"); }
        }

        public void OnLoad()
        {
            Directory.CreateDirectory(PluginDataDir);
            Directory.CreateDirectory(SharedBaselineDir);

            ExporterSettings.Migrate(PluginDataDir, Path.Combine(Config.Instance.DataDir, "HdtCollectionExporterRu"));
            _settings = ExporterSettings.Load(PluginDataDir);
            _exportService = CreateExportService();

            _menuItem = new MenuItem { Header = ExportWindowText.ForLanguage(_settings.Language).IsRussian ? "Коллекция Manacost" : "Manacost collection" };
            _menuItem.Click += delegate { OpenWindow(); };
        }

        public void OnUnload()
        {
            if(_window != null)
            {
                _window.Close();
                _window = null;
            }
            SaveSettings();
        }

        public void OnButtonPress()
        {
            OpenWindow();
        }

        public void OnUpdate()
        {
        }

        private void OpenWindow()
        {
            if(_window == null)
            {
                _window = new ExportWindow(_settings, _exportService, SaveSettings, ExportWindowText.ForLanguage(_settings.Language));
                _window.Closed += delegate { _window = null; };
                _window.Show();
            }
            else
            {
                _window.Activate();
            }
        }

        private void SaveSettings()
        {
            if(_settings != null)
                try { _settings.Save(PluginDataDir); }
                catch(Exception ex) { System.Diagnostics.Trace.TraceError("Collection Exporter settings: " + ex.GetType().Name); }
        }

        internal static string SharedBaselineDir
        {
            get { return Path.Combine(Config.Instance.DataDir, "HdtCollectionExporter"); }
        }

        internal static CollectionExportService CreateExportService()
        {
            var sharedBaselinePath = Path.Combine(SharedBaselineDir, "last-collection-export.json");
            return new CollectionExportService(
                new HdtCollectionProvider(),
                sharedBaselinePath,
                new[]
                {
                    sharedBaselinePath,
                    Path.Combine(Config.Instance.DataDir, "HdtCollectionExporterRu", "last-collection-export.json")
                });
        }
    }

}
