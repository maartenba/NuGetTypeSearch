using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NuGet.Packaging;
using NuGetTypeSearch.Approach3.Indexing.Models;
using NuGetTypeSearch.Bindings;
using NuGetTypeSearch.Bindings.Catalog;
using NuGetTypeSearch.Bindings.Search;

namespace NuGetTypeSearch.Approach3.Indexing
{
    public static class PackageIndexerWithCustomBinding
    {
        private static readonly HttpClient HttpClient = SharedHttpClient.Instance;
        private static readonly JsonSerializer JsonSerializer = JsonSerializer.CreateDefault(
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

        [Disable]
        [FunctionName("Approach3-02-PackageIndexerWithCustomBinding")]
        public static async Task RunAsync(
            [QueueTrigger(Constants.IndexingQueue, Connection = Constants.IndexingQueueConnection)]
            PackageOperation packageOperation,

            [AzureSearchIndex(
                SearchServiceName = Constants.SearchServiceName,
                SearchServiceKey = Constants.SearchServiceKey,
                IndexName = Constants.SearchServiceIndexName,
                IndexDocumentType = typeof(PackageDocument),
                IndexAction = IndexAction.MergeOrUpload,
                CreateOrUpdateIndex = true)]
            IAsyncCollector<PackageDocument> documentAddCollector,

            [AzureSearchIndex(
                SearchServiceName = Constants.SearchServiceName,
                SearchServiceKey = Constants.SearchServiceKey,
                IndexName = Constants.SearchServiceIndexName,
                IndexDocumentType = typeof(PackageDocument),
                IndexAction = IndexAction.Delete,
                CreateOrUpdateIndex = true)]
            IAsyncCollector<PackageDocument> documentDeleteCollector,

            [Blob("index/{Id}.{VersionNormalized}.json", FileAccess.ReadWrite, Connection = Constants.IndexConnection)]
            CloudBlockBlob packageBlob,

            ILogger log)
        {
            // Build actions
            if (packageOperation.IsAdd())
            {
                await RunAddPackageAsync(packageOperation, packageBlob, log, Constants.DevCommitToSearchIndex 
                    ? documentAddCollector
                    : DummyAsyncCollector.For<PackageDocument>());
            }
            else if (packageOperation.IsDelete())
            {
                await RunDeletePackageAsync(packageOperation, packageBlob, Constants.DevCommitToSearchIndex
                    ? documentDeleteCollector
                    : DummyAsyncCollector.For<PackageDocument>());
            }
        }

        private static async Task RunAddPackageAsync(PackageOperation packageOperation, CloudBlockBlob packageBlob, ILogger log, IAsyncCollector<PackageDocument> documentAddCollector)
        {
            var packagesToIndex = new List<PackageDocument>();

            log.LogInformation("Downloading package {packageId}@{packageVersionNormalized} for indexing...",
                packageOperation.Id, packageOperation.Version);

            using (var packageInputStream = await HttpClient.GetStreamAsync(packageOperation.PackageUrl))
            using (var packageInputSeekableStream = TemporaryFileStream.Create())
            {
                await packageInputStream.CopyToAsync(packageInputSeekableStream);
                packageInputSeekableStream.Position = 0;

                log.LogInformation("Finished downloading package {packageId}@{packageVersionNormalized} for indexing...",
                    packageOperation.Id, packageOperation.Version);

                using (var nugetPackage = new PackageArchiveReader(packageInputSeekableStream))
                {
                    log.LogInformation("Analyzing package {packageId}@{packageVersionNormalized}...",
                        packageOperation.Id, packageOperation.Version);

                    // Get some metadata
                    var nuspecReader = nugetPackage.NuspecReader;
                    var packageIdentity = nuspecReader.GetIdentity();
                    var packageSummary = nuspecReader.GetDescription();
                    if (string.IsNullOrEmpty(packageSummary))
                    {
                        packageSummary = nuspecReader.GetSummary();
                    }

                    var packageToIndex = new PackageDocument(
                        packageIdentity.Id,
                        packageIdentity.Version.ToNormalizedString(),
                        packageIdentity.Version.OriginalVersion,
                        nuspecReader.GetTitle(),
                        packageSummary,
                        nuspecReader.GetAuthors(),
                        nuspecReader.GetTags(),
                        nuspecReader.GetIconUrl(),
                        nuspecReader.GetLicenseUrl(),
                        nuspecReader.GetProjectUrl(),
                        packageOperation.Published,
                        AuxiliaryNuGetData.GetDownloadCount(packageIdentity.Id),
                        packageOperation.IsListed,
                        packageIdentity.Version.IsPrerelease);

                    var targetFrameworks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var typeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    var frameworkSpecificGroups = nugetPackage.GetReferenceItems();
                    foreach (var frameworkSpecificGroup in frameworkSpecificGroups)
                    {
                        // Get some metadata
                        var targetFramework = frameworkSpecificGroup.TargetFramework.GetShortFolderName();
                        targetFrameworks.Add(targetFramework);

                        log.LogInformation(
                            "Collecting information for {packageId}@{packageVersionNormalized} and framework {targetFramework}...",
                            packageOperation.Id, packageOperation.Version, targetFramework);

                        // Collect assembly data
                        foreach (var item in frameworkSpecificGroup.Items)
                        {
                            var entry = nugetPackage.GetEntry(item);
                            var entryName = item;

                            log.LogInformation(
                                "Collecting assembly information from {entryName} for {packageId}@{packageVersionNormalized} and framework {targetFramework}...",
                                entryName, packageOperation.Id, packageOperation.Version, targetFramework);

                            using (var assemblyStream = entry.Open())
                            using (var assemblySeekableStream = TemporaryFileStream.Create())
                            {
                                await assemblyStream.CopyToAsync(assemblySeekableStream);
                                assemblySeekableStream.Position = 0;

                                using (var portableExecutableReader = new PEReader(assemblySeekableStream))
                                {
                                    var metadataReader = portableExecutableReader.GetMetadataReader();
                                    foreach (var typeDefinition in metadataReader.TypeDefinitions.Select(metadataReader
                                        .GetTypeDefinition))
                                    {
                                        if (!typeDefinition.Attributes.HasFlag(TypeAttributes.Public)) continue;

                                        var typeNamespace = metadataReader.GetString(typeDefinition.Namespace);
                                        var typeName = metadataReader.GetString(typeDefinition.Name);

                                        if (typeName.StartsWith("<") || typeName.StartsWith("__Static") ||
                                            typeName.Contains("c__DisplayClass")) continue;

                                        log.LogDebug(
                                            "{packageId}@{packageVersionNormalized}, framework {targetFramework}, entry {entryName}: adding {namespace}.{type}",
                                            packageOperation.Id, packageOperation.Version, targetFramework, entryName, typeNamespace, typeName);

                                        typeNames.Add($"{typeNamespace}.{typeName}");
                                    }
                                }
                            }

                            log.LogInformation(
                                "Finished collecting assembly information from {entryName} for {packageId}@{packageVersionNormalized} and framework {targetFramework}.",
                                entryName, packageOperation.Id, packageOperation.Version, targetFramework);
                        }

                        log.LogInformation(
                            "Finished collecting information for {packageId}@{packageVersionNormalized} and framework {targetFramework}.",
                            packageOperation.Id, packageOperation.Version, targetFramework);
                    }

                    packageToIndex.TargetFrameworks = targetFrameworks.ToHashSet();
                    packageToIndex.TypeNames = typeNames.ToHashSet();

                    log.LogInformation("Finished analyzing package {packageId}@{packageVersionNormalized}.",
                        packageOperation.Id, packageOperation.Version);

                    // Add to index blob
                    try
                    {
                        log.LogInformation("Start storing index blob for package {packageId}@{packageVersionNormalized}...",
                            packageOperation.Id, packageOperation.Version);

                        await packageBlob.DeleteIfExistsAsync();

                        using (var jsonStream = await packageBlob.OpenWriteAsync())
                        using (var jsonWriter = new StreamWriter(jsonStream))
                        {
                            JsonSerializer.Serialize(jsonWriter, packagesToIndex);
                        }

                        log.LogInformation("Finished storing index blob for package {packageId}@{packageVersionNormalized}.",
                            packageOperation.Id, packageOperation.Version);
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Error storing index blob for package {packageId}@{packageVersionNormalized}.",
                            packageOperation.Id, packageOperation.Version);
                    }

                    // Add to index
                    try
                    {
                        log.LogInformation("Start storing index document for package {packageId}@{packageVersionNormalized}...",
                            packageOperation.Id, packageOperation.Version);

                        await documentAddCollector.AddAsync(packageToIndex);

                        log.LogInformation("Finished storing index document for package {packageId}@{packageVersionNormalized}.",
                            packageOperation.Id, packageOperation.Version);
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Error storing index document for package {packageId}@{packageVersionNormalized}.",
                            packageOperation.Id, packageOperation.Version);
                    }
                }
            }
        }

        private static async Task RunDeletePackageAsync(PackageOperation packageOperation, CloudBlockBlob packageBlob, IAsyncCollector<PackageDocument> documentDeleteCollector)
        {
            // Delete from index blob
            await packageBlob.DeleteIfExistsAsync();

            // Add delete from index actions
            await documentDeleteCollector.AddAsync(new PackageDocument(packageOperation.Id, packageOperation.Version));
        }
    }
}