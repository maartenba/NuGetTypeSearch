using System.Collections.Generic;
using Newtonsoft.Json;

namespace NuGetTypeSearch.Web.Models
{
    public class FindTypeApiResult
    {
        [JsonProperty("nuGetRoot")]
        public string NuGetRoot { get; set; } = "http://www.nuget.org/packages";

        [JsonProperty("packages")]
        public List<FindTypeApiPackage> Packages { get; set; } = new List<FindTypeApiPackage>();

        [JsonProperty("pageIndex")]
        public long PageIndex { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; } = 20;

        [JsonProperty("totalPages")]
        public long TotalPages { get; set; }

        [JsonProperty("totalResults")]
        public long TotalResults { get; set; }
    }
}