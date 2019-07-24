using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NuGet.Protocol.Catalog;
using NuGet.Protocol.Catalog.Models;
using NuGetTypeSearch.Bindings.Catalog;
using NuGetTypeSearch.Catalog;

namespace NuGetTypeSearch.Approach1
{
    public static class Enqueuer
    {
        private static readonly HttpClient HttpClient = SharedHttpClient.Instance;
        
        [Disable("DISABLE-APPROACH1")]
        [FunctionName("Approach1-Enqueuer"), Singleton(nameof(Enqueuer))]
        public static async Task Run(
            [TimerTrigger("* */1 * * * *", RunOnStartup = true)] TimerInfo timer,
            [Queue(Constants.IndexingQueue, Connection = Constants.IndexingQueueConnection)] ICollector<PackageOperation> queueCollector,
            ILogger logger)
        {
            var cursor = new InMemoryCursor(timer.ScheduleStatus?.Last ?? DateTimeOffset.UtcNow);

            var processor = new CatalogProcessor(
                cursor, 
                new CatalogClient(HttpClient, new NullLogger<CatalogClient>()), 
                new DelegatingCatalogLeafProcessor(
                    added =>
                    {
                        var packageVersion = added.ParsePackageVersion();

                        queueCollector.Add(PackageOperation.ForAdd(
                            added.PackageId,
                            added.PackageVersion,
                            added.VerbatimVersion,
                            packageVersion.ToNormalizedString(),
                            added.Published,
                            string.Format(Constants.NuGetPackageUrlTemplate, added.PackageId, packageVersion.ToNormalizedString()).ToLowerInvariant(),
                            added.IsListed()));

                        return Task.FromResult(true);
                    },
                    deleted =>
                    {
                        queueCollector.Add(PackageOperation.ForDelete(
                            deleted.PackageId, 
                            deleted.PackageVersion,
                            deleted.ParsePackageVersion().ToNormalizedString()));

                        return Task.FromResult(true);
                    }),
                new CatalogProcessorSettings
                {
                    MinCommitTimestamp = timer.ScheduleStatus?.Last ?? DateTimeOffset.UtcNow,
                    MaxCommitTimestamp = timer.ScheduleStatus?.Next ?? DateTimeOffset.UtcNow,
                    ServiceIndexUrl = "https://api.nuget.org/v3/index.json"
                }, 
                new NullLogger<CatalogProcessor>());

            await processor.ProcessAsync(CancellationToken.None);
        }
    }
}