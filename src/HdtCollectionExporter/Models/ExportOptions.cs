namespace HdtCollectionExporter.Models
{
    public class ExportOptions
    {
        public string OutputFolder { get; set; }

        public bool IncludeCardNames { get; set; }

        public bool IncludeGoldenCount { get; set; }

        public bool IncludeMetadata { get; set; }
    }
}
