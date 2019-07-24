using System;

namespace NuGetTypeSearch.Bindings.Catalog
{
    public class PackageOperation
    {
        public static PackageOperation ForAdd(string packageId, string packageVersion, string packageVersionVerbatim, string packageVersionNormalized, DateTimeOffset? packagePublished, string packageUrl, bool isListed) =>
            new PackageOperation
            {
                Id = packageId,
                Version = packageVersion,
                VersionVerbatim = packageVersionVerbatim,
                VersionNormalized = packageVersionNormalized,
                Published = packagePublished,
                PackageUrl = packageUrl,
                IsListed = isListed,
                Action = "add"
            };

        public static PackageOperation ForDelete(string packageId, string packageVersion, string packageVersionNormalized) =>
            new PackageOperation
            {
                Id = packageId,
                Version = packageVersion,
                VersionNormalized = packageVersionNormalized,
                Action = "delete"
            };

        public string Id { get; set; }
        public string Version { get; set; }
        public string VersionVerbatim { get; set; }
        public string VersionNormalized { get; set; }
        public DateTimeOffset? Published { get; set; }
        public string PackageUrl { get; set; }
        public bool IsListed { get; set; }
        public string Action { get; set; }

        public bool IsAdd() => Action == "add";
        public bool IsDelete() => Action == "delete";
    }
}