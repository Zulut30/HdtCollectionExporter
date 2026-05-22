using System;
using System.Collections.Generic;

namespace HdtCollectionExporter.Models
{
    public class ExportResult
    {
        public ExportResult()
        {
            Files = new List<string>();
        }

        public DateTimeOffset ExportedAt { get; set; }

        public int CardCount { get; set; }

        public IList<string> Files { get; private set; }
    }
}
