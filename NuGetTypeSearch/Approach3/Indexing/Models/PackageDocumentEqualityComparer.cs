using System;
using System.Collections.Generic;

namespace NuGetTypeSearch.Approach3.Indexing.Models
{
    public static class PackageDocumentEqualityComparer
    {
        public static IEqualityComparer<PackageDocument> ByPackageId = new PackageIdComparer();
        public static IEqualityComparer<PackageDocument> ByPackageIdAndVersion = new PackageIdAndVersionComparer();
        
        private sealed class PackageIdComparer : IEqualityComparer<PackageDocument>
        {
            public bool Equals(PackageDocument x, PackageDocument y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x.PackageId, y.PackageId, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(PackageDocument obj)
            {
                unchecked
                {
                    return (obj.PackageId != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.PackageId) : 0) * 397;
                }
            }
        }
        
        private sealed class PackageIdAndVersionComparer : IEqualityComparer<PackageDocument>
        {
            public bool Equals(PackageDocument x, PackageDocument y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x.PackageId, y.PackageId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.PackageVersion, y.PackageVersion, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(PackageDocument obj)
            {
                unchecked
                {
                    return ((obj.PackageId != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.PackageId) : 0) * 397) ^ (obj.PackageVersion != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.PackageVersion) : 0);
                }
            }
        }
    }
}