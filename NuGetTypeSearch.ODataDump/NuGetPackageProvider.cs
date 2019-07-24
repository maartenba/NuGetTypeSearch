using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NuGetTypeSearch.ODataDump
{
    public class NuGetPackageProvider
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        private static readonly XNamespace AtomNamespace = "http://www.w3.org/2005/Atom";
        private static readonly XNamespace DataServicesNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices";
        private static readonly XNamespace MetadataNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";

        private readonly TextWriter _log;

        private readonly string _repositoryUrl;

        static NuGetPackageProvider()
        {
            HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NuGetTypeSearch", "1.0.0"));
        }

        public NuGetPackageProvider(string repositoryUrl, TextWriter log)
        {
            _log = log;
            _repositoryUrl = repositoryUrl.TrimEnd('/');
        }

        public async Task GetPackages(DateTime since, Func<IEnumerable<PackageDefinition>, Task> processResults)
        {
            await _log.WriteLineAsync($"Getting packages since: {since}");

            // Fetch first page
            var result = await GetPackagesFromUrl(
                $"{_repositoryUrl}/Packages?$select=Id,Version,NormalizedVersion,LastEdited,Published&$orderby=LastEdited%20desc&$filter=LastEdited%20gt%20datetime%27{since:s}%27");

            await processResults(result.Item2);

            // While there are more pages, fetch more pages
            while (!string.IsNullOrEmpty(result.Item1))
            {
                await _log.WriteLineAsync($"Following OData continuation URL: {result.Item1}.");

                result = await GetPackagesFromUrl(result.Item1);

                await processResults(result.Item2);
            }
        }

        private async Task<Tuple<string, ICollection<PackageDefinition>>> GetPackagesFromUrl(string url)
        {
            await _log.WriteLineAsync($"Retrieving packages from url: {url}...");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            await _log.WriteLineAsync($"Retrieved packages from url: {url}.");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string continuationUrl = null;
                var returnValue = new List<PackageDefinition>();

                using (var odataStream = await response.Content.ReadAsStreamAsync())
                {
                    var odata = XDocument.Load(odataStream);

                    // Parse entries
                    foreach (var entryElement in odata.Root.Elements(AtomNamespace + "entry"))
                    {
                        var propertiesElement = entryElement.Element(MetadataNamespace + "properties");
                        var contentElement = entryElement.Element(AtomNamespace + "content");

                        var published = DateTime.Parse(propertiesElement.Element(DataServicesNamespace + "Published").Value);

                        var packageDefinition = new PackageDefinition
                        {
                            PackageType = "nuget",
                            PackageIdentifier = propertiesElement.Element(DataServicesNamespace + "Id").Value,
                            PackageVersion = propertiesElement.Element(DataServicesNamespace + "Version").Value,
                            PackageVersionNormalized = propertiesElement.Element(DataServicesNamespace + "NormalizedVersion")?.Value,
                            LastEdited = DateTime.Parse(propertiesElement.Element(DataServicesNamespace + "LastEdited").Value),
                            ContentUri = new Uri(contentElement.Attribute("src").Value),
                            IsListed = published.Year != 1900 && published.Year != 1970
                        };

                        returnValue.Add(packageDefinition);
                    }

                    // Parse continuations
                    foreach (var linkElement in odata.Root.Elements(AtomNamespace + "link"))
                    {
                        var linkRel = linkElement.Attribute("rel");
                        if (linkRel != null && linkRel.Value == "next")
                        {
                            continuationUrl = linkElement.Attribute("href").Value;
                            break;
                        }
                    }
                }

                return new Tuple<string, ICollection<PackageDefinition>>(continuationUrl, returnValue);
            }
            else
            {
                await _log.WriteLineAsync($"Error retrieving packages from URL: {url}. Status: {response.StatusCode} - {response.ReasonPhrase}.");

                return new Tuple<string, ICollection<PackageDefinition>>(null, new List<PackageDefinition>());
            }
        }
    }
}