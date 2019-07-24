using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using NuGetTypeSearch.Bindings.Catalog.Bindings;

namespace NuGetTypeSearch.Bindings.Catalog.Configuration
{
    [Extension("NuGetCatalog")]
    internal class NuGetCatalogTriggerExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly NuGetCatalogTriggerAttributeBindingProvider _triggerBindingProvider;

        public NuGetCatalogTriggerExtensionConfigProvider(NuGetCatalogTriggerAttributeBindingProvider triggerBindingProvider)
        {
            _triggerBindingProvider = triggerBindingProvider;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            context.AddBindingRule<NuGetCatalogTriggerAttribute>()
                .BindToTrigger(_triggerBindingProvider);
        }
    }
}