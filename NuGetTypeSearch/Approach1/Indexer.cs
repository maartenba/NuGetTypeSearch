using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NuGetTypeSearch.Bindings.Catalog;

namespace NuGetTypeSearch.Approach1
{
    public static class Indexer
    {
        [Disable("DISABLE-APPROACH1")]
        [FunctionName("Approach1-Indexer")]
        public static void Run(
            [QueueTrigger(Constants.IndexingQueue, Connection = Constants.IndexingQueueConnection)]PackageOperation packageOperation, 
            ILogger logger)
        {
            logger.LogInformation(packageOperation.Action + ": " + packageOperation.Id + "@" + packageOperation.Version);
        }
    }
}
