using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NuGetTypeSearch.Web.Models
{
    public class FindTypeApiPackage
    {
        [JsonProperty("authors")]
        public string Authors { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("downloads")]
        public long Downloads { get; set; }

        [JsonProperty("iconUrl")]
        public string IconUrl { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("isLastPrerelease")]
        public bool IsLastPreRelease { get; set; }

        [JsonProperty("isLastRelease")]
        public bool IsLastRelease { get; set; }

        [JsonProperty("isPrerelease")]
        public bool IsPreRelease { get; set; }

        [JsonProperty("licenseUrl")]
        public string LicenseUrl { get; set; }

        [JsonProperty("match")]
        public FindTypeApiMatch Match { get; set; } = new FindTypeApiMatch();

        [JsonProperty("projectUrl")]
        public string ProjectUrl { get; set; }

        [JsonProperty("published")]
        public DateTimeOffset? Published { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("versionId")]
        public int VersionId { get; set; }
    }
}