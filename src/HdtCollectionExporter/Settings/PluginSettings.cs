using System;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using HdtCollectionExporter.Services;

namespace HdtCollectionExporter.Settings
{
    [Serializable]
    public class PluginSettings
    {
        private const string StorageFileName = "settings.xml";

        public string OutputFolder { get; set; }

        public bool IncludeCardNames { get; set; }

        public bool IncludeGoldenCount { get; set; }

        public bool IncludeMetadata { get; set; }

        public DateTime LastExportTimeUtc { get; set; }

        public string LastStatus { get; set; }

        public double WindowWidth { get; set; }

        public double WindowHeight { get; set; }
        public string Language { get; set; }
        public int FormatIndex { get; set; }
        public int ModeIndex { get; set; }

        public PluginSettings()
        {
            OutputFolder = GetDefaultOutputFolder();
            IncludeCardNames = true;
            IncludeGoldenCount = true;
            IncludeMetadata = true;
            LastExportTimeUtc = DateTime.MinValue;
            LastStatus = "";
            Language = "auto";
            FormatIndex = 2;
            WindowWidth = 940;
            WindowHeight = 790;
        }

        public static PluginSettings Load(string dataDir)
        {
            Directory.CreateDirectory(dataDir);
            var path = Path.Combine(dataDir, StorageFileName);
            if(!File.Exists(path) && !File.Exists(path + ".bak"))
                return new PluginSettings();
            foreach(var candidate in new[] { path, path + ".bak" })
            {
                try
                {
                    using(var stream = File.OpenRead(candidate))
                    {
                        var serializer = new XmlSerializer(typeof(PluginSettings));
                        var settings = serializer.Deserialize(stream) as PluginSettings;
                        if(settings != null) return settings;
                    }
                }
                catch { }
            }
            return new PluginSettings { LastStatus = "SettingsRecovery" };
        }

        public void Save(string dataDir)
        {
            Directory.CreateDirectory(dataDir);
            var path = Path.Combine(dataDir, StorageFileName);
            using(var writer = new StringWriter())
            {
                var serializer = new XmlSerializer(typeof(PluginSettings));
                serializer.Serialize(writer, this);
                // StringWriter declares UTF-16; write that encoding declaration consistently.
                new AtomicFile().Write(path, writer.ToString().Replace("encoding=\"utf-16\"", "encoding=\"utf-8\""), true);
            }
        }

        public static void Migrate(string primaryDir, string russianDir)
        {
            var target = Path.Combine(primaryDir, StorageFileName);
            var source = Path.Combine(russianDir, StorageFileName);
            var marker = Path.Combine(primaryDir, "settings-v2-migrated.txt");
            if(File.Exists(marker)) return;
            if(File.Exists(source) && (!File.Exists(target) || File.GetLastWriteTimeUtc(source) > File.GetLastWriteTimeUtc(target)))
            {
                var settings = Load(russianDir);
                settings.Language = "ru";
                settings.Save(primaryDir);
            }
            new AtomicFile().Write(marker, "Legacy settings preserved.", false);
        }

        public static string GetDefaultOutputFolder()
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if(string.IsNullOrWhiteSpace(documents))
                documents = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            return Path.Combine(documents, "HDT Collection Exports");
        }
    }
}
