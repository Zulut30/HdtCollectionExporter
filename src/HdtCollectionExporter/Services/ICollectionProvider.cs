using System.Threading.Tasks;
using HdtCollectionExporter.Models;

namespace HdtCollectionExporter.Services
{
    public interface ICollectionProvider
    {
        Task<CollectionSnapshot> GetCollectionAsync(ExportOptions options);
    }
}
