using Microsoft.Azure.WebJobs;

namespace NuGetTypeSearch.Bindings.Search.Bindings
{
    internal class AzureSearchAsyncCollectorBuilder<T> : IConverter<AzureSearchIndexAttribute, IAsyncCollector<T>>
        where T : class
    {
        public IAsyncCollector<T> Convert(AzureSearchIndexAttribute attribute)
        {
            return new AzureSearchAsyncCollector<T>(attribute);
        }
    }
}