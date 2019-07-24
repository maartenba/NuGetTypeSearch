using System;
using System.Diagnostics;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;

namespace NuGetTypeSearch.Bindings.Catalog
{
    /// <summary>
    /// Bind a parameter to a NuGet Catalog operation, causing the function to run when a NuGet package is added or deleted.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    [DebuggerDisplay("{ServiceIndexUrl}")]
    [Binding]
    public class NuGetCatalogTriggerAttribute : Attribute, IConnectionProvider
    {
        public string ServiceIndexUrl { get; set; } = "https://api.nuget.org/v3/index.json";

        public bool UseBatchProcessor { get; set; } = false;

        public int PreviousHours { get; set; } = 0;

        [AppSetting]
        public string Connection { get; set; }

        public string CursorContainer { get; set; } = "nugetcatalogtrigger";

        public string CursorBlobName { get; set; } = "cursor.json";
    }
}
