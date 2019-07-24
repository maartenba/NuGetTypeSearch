using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using NuGet.Protocol.Catalog;
using NuGet.Protocol.Catalog.Models;
using NuGet.Versioning;
using NuGetTypeSearch.Catalog;

namespace NuGetTypeSearch.Bindings.Catalog.Listeners
{
    internal class NuGetCatalogListener
        : IListener
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        private readonly ICatalogProcessor _processor;
        
        public NuGetCatalogListener(string serviceIndexUrl, CloudBlockBlob cursorBlob, bool useBatchProcessor, int previousHours, ITriggeredFunctionExecutor executor, ILoggerFactory loggerFactory)
        {
            if (string.IsNullOrEmpty(serviceIndexUrl)) throw new ArgumentNullException(nameof(serviceIndexUrl));
            if (executor == null) throw new ArgumentNullException(nameof(executor));

            async Task<bool> PackageDeleted(PackageDeleteCatalogLeaf deleted)
            {
                await executor.TryExecuteAsync(new TriggeredFunctionData
                {
                    TriggerValue = PackageOperation.ForDelete(
                        deleted.PackageId,
                        deleted.PackageVersion,
                        deleted.ParsePackageVersion().ToNormalizedString()),
                    TriggerDetails = new Dictionary<string, string>()
                }, CancellationToken.None);

                return true;
            }

            async Task<bool> PackageAdded(PackageDetailsCatalogLeaf added)
            {
                var packageVersion = added.ParsePackageVersion();

                await executor.TryExecuteAsync(new TriggeredFunctionData
                {
                    TriggerValue = PackageOperation.ForAdd(
                        added.PackageId, 
                        added.PackageVersion,
                        added.VerbatimVersion, 
                        packageVersion.ToNormalizedString(),
                        added.Published,
                        GeneratePackageUrl(added.PackageId, packageVersion),
                        added.IsListed()),
                    TriggerDetails = new Dictionary<string, string>()
                }, CancellationToken.None);

                return true;
            }

            var minCommitTimeStamp = DateTimeOffset.MinValue;
            if (previousHours > 0)
            {
                minCommitTimeStamp = DateTimeOffset.UtcNow
                    .AddHours(Math.Abs(previousHours) * -1);
            }

            if (!useBatchProcessor)
            {
                _processor = new CatalogProcessor(
                    new CloudBlobCursor(cursorBlob),
                    new CatalogClient(HttpClient, loggerFactory.CreateLogger<CatalogClient>()),
                    new DelegatingCatalogLeafProcessor(PackageAdded, PackageDeleted),
                    new CatalogProcessorSettings { ServiceIndexUrl = serviceIndexUrl, MinCommitTimestamp = minCommitTimeStamp },
                    loggerFactory.CreateLogger<CatalogProcessor>());
            }
            else
            {
                _processor = new BatchCatalogProcessor(
                    new CloudBlobCursor(cursorBlob),
                    new CatalogClient(HttpClient, loggerFactory.CreateLogger<CatalogClient>()),
                    new DelegatingCatalogLeafProcessor(PackageAdded, PackageDeleted),
                    new CatalogProcessorSettings { ServiceIndexUrl = serviceIndexUrl, MinCommitTimestamp = minCommitTimeStamp },
                    loggerFactory.CreateLogger<BatchCatalogProcessor>());
            }
        }

        private static string GeneratePackageUrl(string packageId, NuGetVersion packageVersion) =>
            string.Format(Constants.NuGetPackageUrlTemplate, packageId, packageVersion.ToNormalizedString())
                .ToLowerInvariant();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _processor.ProcessAsync(cancellationToken);

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Cancel()
        {
        }

        public void Dispose()
        {
        }
    }
}