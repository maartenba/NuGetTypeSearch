using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NuGetTypeSearch.Bindings.Catalog;

namespace NuGetTypeSearch.Approach2
{
    public static class Indexer
    {
        [Disable("DISABLE-APPROACH2")]
        [FunctionName("Approach2-Indexer")]
        public static void Run(
            [QueueTrigger(Constants.IndexingQueue, Connection = Constants.IndexingQueueConnection)]PackageOperation packageOperation, 
            ILogger logger)
        {
            logger.LogInformation(packageOperation.Action + ": " + packageOperation.Id + "@" + packageOperation.Version);
        }
    }
}