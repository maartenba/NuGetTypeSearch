using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using System;
using NuGetTypeSearch.Bindings.Search.Bindings;

namespace NuGetTypeSearch.Bindings.Search.Configuration
{
    [Extension("AzureSearch")]
    internal class AzureSearchExtensionConfigProvider : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var bindingRule = context.AddBindingRule<AzureSearchIndexAttribute>();
            bindingRule.BindToCollector<OpenType>(typeof(AzureSearchAsyncCollectorBuilder<>));
        }
    }
}