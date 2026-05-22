using System;
using System.IO;
using System.Xml.Serialization;

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

        public PluginSettings()
        {
            OutputFolder = GetDefaultOutputFolder();
            IncludeCardNames = true;
            IncludeGoldenCount = true;
            IncludeMetadata = true;
            LastExportTimeUtc = DateTime.MinValue;
            LastStatus = "Ready.";
            WindowWidth = 620;
            WindowHeight = 360;
        }

        public static PluginSettings Load(string dataDir)
        {
            Directory.CreateDirectory(dataDir);
            var path = Path.Combine(dataDir, StorageFileName);
            if(!File.Exists(path))
                return new PluginSettings();

            try
            {
                using(var stream = File.OpenRead(path))
                {
                    var serializer = new XmlSerializer(typeof(PluginSettings));
                    var settings = serializer.Deserialize(stream) as PluginSettings;
                    return settings ?? new PluginSettings();
                }
            }
            catch
            {
                return new PluginSettings
                {
                    LastStatus = "Settings could not be loaded; defaults were used."
                };
            }
        }

        public void Save(string dataDir)
        {
            Directory.CreateDirectory(dataDir);
            var path = Path.Combine(dataDir, StorageFileName);
            using(var stream = File.Create(path))
            {
                var serializer = new XmlSerializer(typeof(PluginSettings));
                serializer.Serialize(stream, this);
            }
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
