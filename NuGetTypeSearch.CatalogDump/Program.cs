using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NuGet.Protocol.Catalog;
using NuGet.Protocol.Catalog.Models;
using NuGetTypeSearch.Catalog;

namespace NuGetTypeSearch.CatalogDump
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var httpClient = new HttpClient();
            var cursor = new InMemoryCursor(null);

            var processor = new BatchCatalogProcessor(
                cursor, 
                new CatalogClient(httpClient, new NullLogger<CatalogClient>()), 
                new DelegatingCatalogLeafProcessor(
                    added =>
                    {
                        var packageVersion = added.ParsePackageVersion();

                        Console.WriteLine("[ADDED] {2} - {0}@{1}", added.PackageId, packageVersion.ToNormalizedString(), added.Created);

                        return Task.FromResult(true);
                    },
                    deleted =>
                    {
                        var packageVersion = deleted.ParsePackageVersion();
                        
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("[DELETED] ");
                        Console.ResetColor();
                        Console.WriteLine("{2} - {0}@{1}", deleted.PackageId, packageVersion.ToNormalizedString(), deleted.CommitTimestamp);

                        return Task.FromResult(true);
                    }),
                new CatalogProcessorSettings
                {
                    MinCommitTimestamp = DateTimeOffset.MinValue,
                    ServiceIndexUrl = "https://api.nuget.org/v3/index.json"
                }, 
                new NullLogger<BatchCatalogProcessor>());

            await processor.ProcessAsync(CancellationToken.None);
        }
    }
}