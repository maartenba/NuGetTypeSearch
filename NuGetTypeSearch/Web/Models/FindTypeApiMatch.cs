using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NuGetTypeSearch.Web.Models
{
    public class FindTypeApiMatch
    {
        [JsonProperty("assemblies")]
        public HashSet<string> Assemblies { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        [JsonProperty("platforms")]
        public HashSet<string> Platforms { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        [JsonProperty("typeNames")]
        public HashSet<FindTypeApiMatchTypeName> TypeNames { get; set; } = new HashSet<FindTypeApiMatchTypeName>(FindTypeApiMatchTypeName.DefaultComparer);
    }
}