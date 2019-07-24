using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NuGetTypeSearch.Web.Models
{
    public class FindTypeApiMatchTypeName
    {
        [JsonProperty("kind")]
        public int Kind { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("namespace")]
        public string Namespace { get; set; }
        
        public static IEqualityComparer<FindTypeApiMatchTypeName> DefaultComparer { get; } = new DefaultEqualityComparer();

        private sealed class DefaultEqualityComparer : IEqualityComparer<FindTypeApiMatchTypeName>
        {
            public bool Equals(FindTypeApiMatchTypeName x, FindTypeApiMatchTypeName y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Kind == y.Kind && string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Namespace, y.Namespace, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(FindTypeApiMatchTypeName obj)
            {
                unchecked
                {
                    var hashCode = obj.Kind;
                    hashCode = (hashCode * 397) ^ (obj.Name != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name) : 0);
                    hashCode = (hashCode * 397) ^ (obj.Namespace != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Namespace) : 0);
                    return hashCode;
                }
            }
        }
    }
}