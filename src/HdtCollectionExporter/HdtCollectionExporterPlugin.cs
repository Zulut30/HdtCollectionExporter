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
            get { return new Version(1, 4, 0); }
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

            _settings = ExporterSettings.Load(PluginDataDir);
            _exportService = CreateExportService();

            _menuItem = new MenuItem { Header = Name };
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
                _window = new ExportWindow(_settings, _exportService, SaveSettings, ExportWindowText.English());
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
                _settings.Save(PluginDataDir);
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

    public class HdtCollectionExporterRussianPlugin : IPlugin
    {
        private MenuItem _menuItem;
        private ExportWindow _window;
        private ExporterSettings _settings;
        private CollectionExportService _exportService;

        public string Name
        {
            get { return "\u042d\u043a\u0441\u043f\u043e\u0440\u0442 \u043a\u043e\u043b\u043b\u0435\u043a\u0446\u0438\u0438 \u043e\u0442 Manacost"; }
        }

        public string Description
        {
            get { return "\u042d\u043a\u0441\u043f\u043e\u0440\u0442\u0438\u0440\u0443\u0435\u0442 \u043a\u043e\u043b\u043b\u0435\u043a\u0446\u0438\u044e Hearthstone \u0438\u0437 HDT \u0432 \u043b\u043e\u043a\u0430\u043b\u044c\u043d\u044b\u0435 JSON \u0438 CSV \u0444\u0430\u0439\u043b\u044b. \u041f\u043b\u0430\u0433\u0438\u043d \u043e\u0442 \u043a\u043e\u043c\u0430\u043d\u0434\u044b Manacost."; }
        }

        public string ButtonText
        {
            get { return "\u041e\u0442\u043a\u0440\u044b\u0442\u044c \u044d\u043a\u0441\u043f\u043e\u0440\u0442"; }
        }

        public string Author
        {
            get { return "Manacost"; }
        }

        public Version Version
        {
            get { return new Version(1, 4, 0); }
        }

        public MenuItem MenuItem
        {
            get { return _menuItem; }
        }

        internal static string PluginDataDir
        {
            get { return Path.Combine(Config.Instance.DataDir, "HdtCollectionExporterRu"); }
        }

        public void OnLoad()
        {
            Directory.CreateDirectory(PluginDataDir);
            Directory.CreateDirectory(HdtCollectionExporterPlugin.SharedBaselineDir);

            _settings = ExporterSettings.Load(PluginDataDir);
            _exportService = HdtCollectionExporterPlugin.CreateExportService();

            _menuItem = new MenuItem { Header = Name };
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
                _window = new ExportWindow(_settings, _exportService, SaveSettings, ExportWindowText.Russian());
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
                _settings.Save(PluginDataDir);
        }
    }
}
