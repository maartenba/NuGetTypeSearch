using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace NuGetTypeSearch.Approach3.Enqueueing.Models
{
    public class PackageEntity
        : TableEntity
    {
        public string Id
        {
            get { return PartitionKey; }
            set { PartitionKey = value; }
        }

        public string Version
        {
            get { return RowKey; }
            set { RowKey = value; }
        }

        public string VersionVerbatim { get; set; }
        public string VersionNormalized { get; set; }
        public DateTimeOffset? Published { get; set; }
        public string Url { get; set; }
        public bool IsListed { get; set; }
    }
}