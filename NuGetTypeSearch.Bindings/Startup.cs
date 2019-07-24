using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NuGetTypeSearch.Bindings;
using NuGetTypeSearch.Bindings.Catalog.Bindings;
using NuGetTypeSearch.Bindings.Catalog.Configuration;
using NuGetTypeSearch.Bindings.Search.Configuration;

[assembly: WebJobsStartup(typeof(Startup))]

namespace NuGetTypeSearch.Bindings
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.Services.AddSingleton<NuGetCatalogTriggerAttributeBindingProvider>();

            builder.AddExtension<NuGetCatalogTriggerExtensionConfigProvider>();
            builder.AddExtension<AzureSearchExtensionConfigProvider>();
        }
    }
}