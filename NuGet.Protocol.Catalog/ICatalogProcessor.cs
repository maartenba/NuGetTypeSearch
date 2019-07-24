using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Protocol.Catalog
{
    public interface ICatalogProcessor
    {
        Task<bool> ProcessAsync(CancellationToken cancellationToken);
    }
}