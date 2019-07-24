using System;
using Newtonsoft.Json;

namespace NuGetTypeSearch.ODataDump
{
    public class PackageDefinition
    {
        public string PackageType { get; set; }
        public string PackageIdentifier { get; set; }
        public string PackageVersion { get; set; }
        public string PackageVersionNormalized { get; set; }

        public Uri ContentUri { get; set; }

        public bool IsListed { get; set; }

        public DateTime LastEdited { get; set; }

        public string AsJsonObject()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }

        public string AsCsv()
        {
            return $"{PackageIdentifier};{PackageVersion};{PackageVersionNormalized ?? ""};\"{ContentUri}\";{IsListed};{LastEdited:O}";
        }
    }
}