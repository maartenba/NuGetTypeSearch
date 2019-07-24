using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using NeoSmart.Utils;

namespace NuGetTypeSearch.Approach3.Indexing.Models
{
    [SerializePropertyNamesAsCamelCase]
    public class PackageDocument
    {
        public PackageDocument() { }

        public PackageDocument(
            string packageId,
            string packageVersion)
            : this(
                packageId,
                packageVersion,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                0,
                false,
                false,
                new HashSet<string>(),
                new HashSet<string>())
        { }

        public PackageDocument(
            string packageId,
            string packageVersion,
            string packageVersionVerbatim,
            string title,
            string summary,
            string authors,
            string tags,
            string iconUrl,
            string licenseUrl,
            string projectUrl,
            DateTimeOffset? published,
            long downloadCount,
            bool isListed,
            bool isPreRelease) 
            : this(
                packageId,
                packageVersion,
                packageVersionVerbatim,
                title,
                summary,
                authors,
                tags,
                iconUrl,
                licenseUrl,
                projectUrl,
                published,
                downloadCount,
                isListed,
                isPreRelease,
                new HashSet<string>(), 
                new HashSet<string>()) { }

        public PackageDocument(
            string packageId,
            string packageVersion,
            string packageVersionVerbatim,
            string title,
            string summary,
            string authors,
            string tags,
            string iconUrl,
            string licenseUrl,
            string projectUrl,
            DateTimeOffset? published,
            long downloadCount,
            bool isListed,
            bool isPreRelease,
            HashSet<string> targetFrameworks,
            HashSet<string> typeNames)
        {
            RawIdentifier = packageId + "@" + packageVersion;
            Identifier = UrlBase64.Encode(Encoding.UTF8.GetBytes(RawIdentifier));

            PackageId = packageId;
            PackageVersion = packageVersion;
            PackageVersionVerbatim = packageVersionVerbatim;
            Title = title;
            Summary = summary;
            Authors = authors;
            Tags = tags;
            IconUrl = iconUrl;
            LicenseUrl = licenseUrl;
            ProjectUrl = projectUrl;
            Published = published;
            DownloadCount = downloadCount;
            IsListed = isListed;
            IsPreRelease = isPreRelease;
            TargetFrameworks = targetFrameworks;
            TypeNames = typeNames;
        }

        public string RawIdentifier { get; set; }

        [Key]
        public string Identifier { get; set; }

        [IsSearchable, IsSortable, IsFacetable, IsRetrievable(true)]
        public string PackageId { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsRetrievable(true)]
        public string PackageVersion { get; set; }

        [IsRetrievable(true)]
        public string PackageVersionVerbatim { get; set; }

        [IsRetrievable(true)]
        public string Title { get; set; }

        [IsRetrievable(true)]
        public string Summary { get; set; }

        [IsRetrievable(true)]
        public string Authors { get; set; }

        [IsRetrievable(true)]
        public string Tags { get; set; }

        [IsRetrievable(true)]
        public string IconUrl { get; set; }

        [IsRetrievable(true)]
        public string LicenseUrl { get; set; }

        [IsRetrievable(true)]
        public string ProjectUrl { get; set; }

        [IsRetrievable(true)]
        public DateTimeOffset? Published { get; set; }

        [IsRetrievable(true)]
        public long DownloadCount { get; set; }

        [IsFilterable, IsRetrievable(true)]
        public bool? IsListed { get; set; }

        [IsFilterable, IsRetrievable(true)]
        public bool? IsPreRelease { get; set; }

        [IsSearchable, IsRetrievable(true)]
        public HashSet<string> TargetFrameworks { get; set; } = new HashSet<string>();

        [IsSearchable, IsRetrievable(true), Analyzer("simple")]
        public HashSet<string> TypeNames { get; set; } = new HashSet<string>();
    }
}