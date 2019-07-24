using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NuGetTypeSearch.Bindings.Catalog;

namespace NuGetTypeSearch.Approach2
{
    public static class Enqueuer
    {
        [Disable("DISABLE-APPROACH2")]
        [FunctionName("Approach2-Enqueuer")]
        [Singleton(Mode = SingletonMode.Listener)]
        public static void Run(
            [NuGetCatalogTrigger(CursorBlobName = nameof(Enqueuer))] PackageOperation packageOperation,
            [Queue(Constants.IndexingQueue, Connection = Constants.IndexingQueueConnection)] ICollector<PackageOperation> queueCollector,
            ILogger logger)
        {
            // Log
            logger.LogInformation(packageOperation.Action + ": " + packageOperation.Id + "@" + packageOperation.Version);

            // Forward...
            queueCollector.Add(packageOperation);
        }
    }
}