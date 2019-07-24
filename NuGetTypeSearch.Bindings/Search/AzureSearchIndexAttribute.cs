using System;
using System.Diagnostics;
using Microsoft.Azure.WebJobs.Description;

namespace NuGetTypeSearch.Bindings.Search
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [DebuggerDisplay("{SearchServiceName} {IndexName}")]
    [Binding]
    public class AzureSearchIndexAttribute : Attribute
    {
        public string SearchServiceName { get; set; }

        public string SearchServiceKey { get; set; }

        public string IndexName { get; set; }

        public Type IndexDocumentType { get; set; }

        public IndexAction IndexAction { get; set; }

        public bool CreateOrUpdateIndex { get; set; } = false;
    }
}
