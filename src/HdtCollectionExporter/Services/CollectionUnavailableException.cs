using System;

namespace HdtCollectionExporter.Services
{
    public class CollectionUnavailableException : Exception
    {
        public CollectionUnavailableException(string message) : base(message)
        {
        }

        public CollectionUnavailableException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class NoPreviousExportException : Exception
    {
        public NoPreviousExportException(string message) : base(message)
        {
        }
    }
}
