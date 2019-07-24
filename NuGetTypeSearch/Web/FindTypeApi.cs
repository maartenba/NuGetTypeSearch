using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using NuGet.Frameworks;
using NuGet.Versioning;
using NuGetTypeSearch.Approach3.Indexing.Models;
using NuGetTypeSearch.Web.Models;

namespace NuGetTypeSearch.Web
{
    // SEE http://resharper-nugetsearch.jetbrains.com/api/v1/find-type?name=%4A%4Fbject&caseSensitive=true
    [SuppressMessage("ReSharper", "PossibleLossOfFraction")]
    public static class FindTypeApi
    {
        private const int PageSize = 20;
        private const int MaxResults = 80;
        private static readonly IFrameworkNameProvider FrameworkNameProvider = DefaultFrameworkNameProvider.Instance;

        private static readonly SearchServiceClient SearchServiceClient = new SearchServiceClient(Constants.SearchServiceName, new SearchCredentials(Constants.SearchServiceKey));

        static FindTypeApi()
        {
            // Ensure index exists
            SearchServiceClient.Indexes.CreateOrUpdate(new Index
            {
                Name = Constants.SearchServiceIndexName,
                Fields = FieldBuilder.BuildForType<PackageDocument>()
            });
        }

        [FunctionName("Web-FindTypeApi")]
        public static Task<IActionResult> RunFindTypeApiAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/find-type")] HttpRequest request,
            ILogger log)
        {
            // Fetch parameters
            var name = request.Query["name"].ToString();

            return RunInternalAsync(request, name, null, log);
        }

        [FunctionName("Web-FindNamespaceApi")]
        public static Task<IActionResult> RunFindNamespaceAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/find-namespace")] HttpRequest request,
            ILogger log)
        {
            // Fetch parameters
            var name = request.Query["name"].ToString();

            return RunInternalAsync(request, null, name, log);
        }
      
        private static async Task<IActionResult> RunInternalAsync(
            HttpRequest request,
            string typeName,
            string typeNamespace,
            ILogger log)
        {
            // Fetch parameters
            var allowPrerelease = ParseQuery(request.Query, "allowPrerelease", false);
            var latestVersion = ParseQuery(request.Query, "latestVersion", true);
            var page = ParseQuery(request.Query, "pageIndex", 0);
            
            // Build query
            var searchText = !string.IsNullOrEmpty(typeName)
                ? $"typeNames:.{typeName}"
                : $"typeNames:{typeNamespace}.";

            // Search
            var indexClient = SearchServiceClient.Indexes.GetClient(Constants.SearchServiceIndexName);

            log.LogInformation("Performing search for {searchText}...", searchText);

            var resultsCollector = new HashSet<PackageDocument>(latestVersion 
                ? PackageDocumentEqualityComparer.ByPackageId
                : PackageDocumentEqualityComparer.ByPackageIdAndVersion);
            
            await indexClient.Documents.ForEachAsync<PackageDocument>(searchText,
                documentResult =>
                {
                    var isPreRelease = documentResult.Document.IsPreRelease.HasValue &&
                                       documentResult.Document.IsPreRelease.Value;
                    if (allowPrerelease || !isPreRelease)
                    {
                        resultsCollector.Add(documentResult.Document);
                    }

                    return resultsCollector.Count <= MaxResults;
                },
                new SearchParameters
                {
                    Filter = "isListed eq true" + (!allowPrerelease ? " and isPreRelease eq false" : ""),
                    OrderBy = new List<string> { "search.score() desc", "packageVersion desc" },
                    QueryType = QueryType.Full
                });
            
            log.LogInformation("Finished performing search for {searchText}. Total results: {totalResults} (page: {page})", searchText, resultsCollector.Count, page);

            // Build result
            var result = new FindTypeApiResult();
            result.TotalResults = resultsCollector.Count;
            result.PageIndex = page;
            result.PageSize = PageSize;
            result.TotalPages = (long)Math.Ceiling((decimal)(resultsCollector.Count / PageSize) + 1);

            foreach (var searchResult in resultsCollector.Take(PageSize))
            {
                var nugetVersion = NuGetVersion.Parse(searchResult.PackageVersion);

                var resultItem = new FindTypeApiPackage
                {
                    Id = searchResult.PackageId,
                    Version = nugetVersion.ToNormalizedString(),
                    VersionId = 1,
                    Title = searchResult.Title,
                    Authors = searchResult.Authors,
                    Description = searchResult.Summary,
                    Downloads = searchResult.DownloadCount,
                    IconUrl = !string.IsNullOrEmpty(searchResult.IconUrl) ? searchResult.IconUrl : Constants.NuGetPackageDefaultIconUrl,
                    LicenseUrl = searchResult.LicenseUrl,
                    ProjectUrl = searchResult.ProjectUrl,
                    Published = searchResult.Published,
                    Tags = (searchResult.Tags ?? "").Split(' ').ToList(),

                    IsPreRelease = nugetVersion.IsPrerelease,
                    IsLastPreRelease = nugetVersion.IsPrerelease,
                    IsLastRelease = !nugetVersion.IsPrerelease
                };

                foreach (var targetFramework in searchResult.TargetFrameworks)
                {
                    var framework = NuGetFramework.Parse(targetFramework, FrameworkNameProvider);
                    resultItem.Match.Platforms.Add(framework.DotNetFrameworkName);
                }

                foreach (var searchResultTypeName in searchResult.TypeNames)
                {
                    var resultNamespace = searchResultTypeName.SubstringUntilLast(".");
                    var resultType = searchResultTypeName.SubstringAfterLast(".");

                    if (!string.IsNullOrEmpty(resultNamespace))
                    {
                        resultItem.Match.Assemblies.Add(resultNamespace);
                        if (!string.IsNullOrEmpty(typeName) && resultType.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                        {
                            resultItem.Match.TypeNames.Add(new FindTypeApiMatchTypeName { Kind = 0, Namespace = resultNamespace, Name = resultType });
                        }
                        else if (!string.IsNullOrEmpty(typeNamespace) && resultNamespace.Equals(typeNamespace, StringComparison.OrdinalIgnoreCase))
                        {
                            resultItem.Match.TypeNames.Add(new FindTypeApiMatchTypeName { Kind = 0, Namespace = resultNamespace });
                        }
                    }
                }

                if (resultItem.Match.TypeNames.Count > 0)
                {
                    result.Packages.Add(resultItem);
                }
            }

            return new OkObjectResult(result);
        }

        private static bool ParseQuery(IQueryCollection query, string parameterName, bool defaultValue)
        {
            var value = query[parameterName].ToString();
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            return bool.TryParse(value, out var v) && v;
        }

        private static int ParseQuery(IQueryCollection query, string parameterName, int defaultValue)
        {
            var value = query[parameterName].ToString();
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            return int.TryParse(value, out var p) ? p : defaultValue;
        }
    }
}
