using System;

namespace HdtCollectionExporter.Models
{
    public class BaselineStatus
    {
        public bool Exists { get; set; }

        public string Path { get; set; }

        public string ExportedAt { get; set; }

        public int CardCount { get; set; }

        public DateTime LastWriteTimeUtc { get; set; }
    }
}
