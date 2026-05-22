using System;

namespace HdtCollectionExporter.Models
{
    [Flags]
    public enum ExportFormat
    {
        Json = 1,
        Csv = 2,
        Both = Json | Csv
    }
}
