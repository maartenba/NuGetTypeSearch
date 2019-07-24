using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using NuGetTypeSearch.Approach3.Enqueueing.Models;
using NuGetTypeSearch.Bindings.Catalog;

namespace NuGetTypeSearch.Approach3.Enqueueing
{
    public static class PopulateQueueAndTable
    {
        [Disable("DISABLE-APPROACH3")]
        [FunctionName("Approach3-01-PopulateQueueAndTable")]
        [Singleton(Mode = SingletonMode.Listener)]
        public static async Task RunAsync(
            [NuGetCatalogTrigger(CursorBlobName = "catalogCursor.json", UseBatchProcessor = true)] PackageOperation packageOperation,
            [Queue(Constants.IndexingQueue, Connection = Constants.IndexingQueueConnection)] ICollector<PackageOperation> indexingQueueCollector,
            [Queue(Constants.DownloadingQueue, Connection = Constants.DownloadingQueueConnection)] ICollector<PackageOperation> downloadingQueueCollector,
            [Table("packages")] CloudTable outputTable,
            ILogger log)
        {
            // Log
            log.LogInformation("Appending package {action} operation for {packageId}@{packageVersionNormalized}...", packageOperation.Action, packageOperation.Id, packageOperation.Version);

            // Append to queues
            indexingQueueCollector.Add(packageOperation);
            downloadingQueueCollector.Add(packageOperation);

            // Store (or delete) from table
            var entity = new PackageEntity
            {
                Id = packageOperation.Id,
                Version = packageOperation.Version,
                VersionVerbatim = packageOperation.VersionVerbatim,
                VersionNormalized = packageOperation.VersionNormalized,
                Published = packageOperation.Published,
                Url = packageOperation.PackageUrl,
                IsListed = packageOperation.IsListed,
                Timestamp = DateTimeOffset.UtcNow
            };

            if (packageOperation.IsAdd())
            {
                await outputTable.ExecuteAsync(TableOperation.InsertOrMerge(entity));
            }
            else
            {
                await outputTable.ExecuteAsync(TableOperation.Delete(entity));
            }

            log.LogInformation("Finished appending package {action} operation for {packageId}@{packageVersionNormalized}.", packageOperation.Action, packageOperation.Id, packageOperation.Version);
        }
    }
}
