using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;

namespace NuGetTypeSearch.Bindings.Catalog.Bindings
{
    internal class NuGetCatalogTriggerAttributeBindingProvider 
        : ITriggerBindingProvider
    {
        private readonly INameResolver _nameResolver;
        private readonly StorageAccountProvider _accountProvider;
        private readonly ILoggerFactory _loggerFactory;

        public NuGetCatalogTriggerAttributeBindingProvider(
            INameResolver nameResolver,
            StorageAccountProvider accountProvider,
            ILoggerFactory loggerFactory)
        {
            _nameResolver = nameResolver;
            _accountProvider = accountProvider;
            _loggerFactory = loggerFactory;
        }

        public async Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var parameter = context.Parameter;
            var attribute = parameter.GetCustomAttribute<NuGetCatalogTriggerAttribute>(false);
            if (attribute == null)
            {
                return null;
            }

            if (!IsSupportBindingType(parameter.ParameterType))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    "Can't bind NuGetCatalogTriggerAttribute to type '{0}'.", parameter.ParameterType));
            }

            var account = _accountProvider.Get(attribute.Connection, _nameResolver);
            var blobClient = account.CreateCloudBlobClient();
            var cursorContainer = blobClient.GetContainerReference(attribute.CursorContainer);

            await cursorContainer.CreateIfNotExistsAsync();

            var cursorBlob = cursorContainer.GetBlockBlobReference(attribute.CursorBlobName);

            return new NuGetCatalogTriggerBinding(parameter, attribute.ServiceIndexUrl, attribute.UseBatchProcessor, attribute.PreviousHours, cursorBlob, _loggerFactory);
        }


        public bool IsSupportBindingType(Type bindingType)
        {
            return bindingType == typeof(PackageOperation) || bindingType == typeof(string);
        }
    }
}