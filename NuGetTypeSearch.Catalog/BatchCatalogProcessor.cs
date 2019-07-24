using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Protocol;
using NuGet.Protocol.Catalog;
using NuGet.Protocol.Catalog.Models;
using NuGet.Protocol.Core.Types;

namespace NuGetTypeSearch.Catalog
{
    public class BatchCatalogProcessor 
        : ICatalogProcessor
    {
        private const string CatalogResourceType = "Catalog/3.0.0";
        private const int BatchSize = 12;
        private readonly ICatalogLeafProcessor _leafProcessor;
        private readonly ICatalogClient _client;
        private readonly ICursor _cursor;
        private readonly ILogger<BatchCatalogProcessor> _logger;
        private readonly CatalogProcessorSettings _settings;
        private readonly SemaphoreSlim _throttle = new SemaphoreSlim(Environment.ProcessorCount * 8);

        public BatchCatalogProcessor(
            ICursor cursor,
            ICatalogClient client,
            ICatalogLeafProcessor leafProcessor,
            CatalogProcessorSettings settings,
            ILogger<BatchCatalogProcessor> logger)
        {
            _leafProcessor = leafProcessor ?? throw new ArgumentNullException(nameof(leafProcessor));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _cursor = cursor ?? throw new ArgumentNullException(nameof(cursor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (settings.ServiceIndexUrl == null)
            {
                throw new ArgumentException(
                    $"The {nameof(CatalogProcessorSettings.ServiceIndexUrl)} property of the " +
                    $"{nameof(CatalogProcessorSettings)} must not be null.",
                    nameof(settings));
            }

            // Clone the settings to avoid mutability issues.
            _settings = settings.Clone();
        }

        public async Task<bool> ProcessAsync(CancellationToken cancellationToken)
        {
            var catalogIndexUrl = await GetCatalogIndexUrlAsync();

            var minCommitTimestamp = await GetMinCommitTimestamp();
            _logger.LogInformation(
                "Using time bounds {min:O} (exclusive) to {max:O} (inclusive).",
                minCommitTimestamp,
                _settings.MaxCommitTimestamp);

            return await ProcessIndexAsync(catalogIndexUrl, minCommitTimestamp, cancellationToken);
        }

        private async Task<bool> ProcessIndexAsync(string catalogIndexUrl, DateTimeOffset minCommitTimestamp, CancellationToken cancellationToken)
        {
            var index = await _client.GetIndexAsync(catalogIndexUrl);

            // Fetch pages for processing
            var pageItems = index.GetPagesInBounds(
                minCommitTimestamp,
                _settings.MaxCommitTimestamp)
                .Take(BatchSize).ToList();
            _logger.LogInformation(
                "{pages} pages were in the time bounds, out of {totalPages}.",
                pageItems.Count,
                index.Items.Count);

            if (pageItems.Count == 0) return true;

            var success = true;
            var latestCommit = pageItems.Max(page => page.CommitTimestamp);

            // Fetch all catalog pages
            var pageItemTasks = new List<Task<CatalogPage>>();
            foreach (var pageItem in pageItems)
            {
                pageItemTasks.Add(GetPageAsync(pageItem.Url, cancellationToken));
            }

            var catalogPages = await Task.WhenAll(pageItemTasks);
            var leavesToProcess = catalogPages
                .Where(catalogPage => catalogPage != null)
                .SelectMany(
                    catalogPage => catalogPage.GetLeavesInBounds(
                        minCommitTimestamp,
                        _settings.MaxCommitTimestamp,
                        _settings.ExcludeRedundantLeaves))
                .GroupBy(package => package.PackageId + "-" + package.PackageVersion)
                .Select(group => group.OrderByDescending(package => package.CommitTimestamp).First());

            // Process leaves
            var leafTasks = new List<Task<bool>>();
            foreach (var leafItem in leavesToProcess)
            {
                leafTasks.Add(ProcessLeafAsync(leafItem, cancellationToken));
            }

            if (leafTasks.Count == 0) return true;

            var leafResults = await Task.WhenAll(leafTasks);
            // ReSharper disable once RedundantBoolCompare
            success = leafResults.All(result => result == true);

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Stop processing because of cancellation request.");
                success = false;
            }

            if (success)
            {
                await _cursor.SetAsync(latestCommit);
            }

            return success;
        }

        private async Task<CatalogPage> GetPageAsync(string pageUrl, CancellationToken cancellationToken)
        {
            try
            {
                await _throttle.WaitAsync(cancellationToken);
                if (!cancellationToken.IsCancellationRequested)
                {
                    return await _client.GetPageAsync(pageUrl);
                }

                return null;
            }
            finally
            {
                _throttle.Release();
            }
        }

        private async Task<bool> ProcessLeafAsync(CatalogLeafItem leafItem, CancellationToken cancellationToken)
        {
            bool success;
            try
            {
                await _throttle.WaitAsync(cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                switch (leafItem.Type)
                {
                    case CatalogLeafType.PackageDelete:
                        var packageDelete = await _client.GetPackageDeleteLeafAsync(leafItem.Url);
                        success = await _leafProcessor.ProcessPackageDeleteAsync(packageDelete);
                        break;
                    case CatalogLeafType.PackageDetails:
                        var packageDetails = await _client.GetPackageDetailsLeafAsync(leafItem.Url);
                        success = await _leafProcessor.ProcessPackageDetailsAsync(packageDetails);
                        break;
                    default:
                        throw new NotSupportedException($"The catalog leaf type '{leafItem.Type}' is not supported.");
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    0,
                    exception,
                    "An exception was thrown while processing leaf {leafUrl}.",
                    leafItem.Url);
                success = false;
            }
            finally
            {
                _throttle.Release();
            }

            if (!success)
            {
                _logger.LogWarning(
                    "Failed to process leaf {leafUrl} ({packageId} {packageVersion}, {leafType}).",
                    leafItem.Url,
                    leafItem.PackageId,
                    leafItem.PackageVersion,
                    leafItem.Type);
            }

            return success;
        }

        private async Task<DateTimeOffset> GetMinCommitTimestamp()
        {
            var minCommitTimestamp = await _cursor.GetAsync();

            minCommitTimestamp = minCommitTimestamp
                ?? _settings.DefaultMinCommitTimestamp
                ?? _settings.MinCommitTimestamp;

            if (minCommitTimestamp.Value < _settings.MinCommitTimestamp)
            {
                minCommitTimestamp = _settings.MinCommitTimestamp;
            }

            return minCommitTimestamp.Value;
        }

        private async Task<string> GetCatalogIndexUrlAsync()
        {
            _logger.LogInformation("Getting catalog index URL from {serviceIndexUrl}.", _settings.ServiceIndexUrl);
            string catalogIndexUrl;
            var sourceRepository = Repository.Factory.GetCoreV3(_settings.ServiceIndexUrl, FeedType.HttpV3);
            var serviceIndexResource = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
            catalogIndexUrl = serviceIndexResource.GetServiceEntryUri(CatalogResourceType)?.AbsoluteUri;
            if (catalogIndexUrl == null)
            {
                throw new InvalidOperationException(
                    $"The service index does not contain resource '{CatalogResourceType}'.");
            }

            return catalogIndexUrl;
        }
    }
}
