using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGetTypeSearch.Bindings.Catalog.Listeners;

namespace NuGetTypeSearch.Bindings.Catalog.Bindings
{
    internal class NuGetCatalogTriggerBinding : ITriggerBinding
    {
        private readonly ParameterInfo _parameter;
        private readonly string _serviceIndexUrl;
        private readonly bool _useBatchProcessor;
        private readonly int _previousHours;
        private readonly CloudBlockBlob _cursorBlob;
        private readonly ILoggerFactory _loggerFactory;

        public NuGetCatalogTriggerBinding(ParameterInfo parameter, string serviceIndexUrl, bool useBatchProcessor, int previousHours, CloudBlockBlob cursorBlob, ILoggerFactory loggerFactory)
        {
            _parameter = parameter;
            _serviceIndexUrl = serviceIndexUrl;
            _useBatchProcessor = useBatchProcessor;
            _previousHours = previousHours;
            _cursorBlob = cursorBlob;
            _loggerFactory = loggerFactory;
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            return Task.FromResult<IListener>(
                new NuGetCatalogListener(_serviceIndexUrl, _cursorBlob, _useBatchProcessor, _previousHours, context.Executor, _loggerFactory));
        }

        public Type TriggerValueType => typeof(PackageOperation);

        public IReadOnlyDictionary<string, Type> BindingDataContract 
            => new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                {"data", typeof(JObject)},
                {nameof(PackageOperation.Id), typeof(string)},
                {nameof(PackageOperation.Version), typeof(string)},
                {nameof(PackageOperation.VersionVerbatim), typeof(string)},
                {nameof(PackageOperation.VersionNormalized), typeof(string)},
                {nameof(PackageOperation.Published), typeof(DateTimeOffset?)},
                {nameof(PackageOperation.PackageUrl), typeof(string)},
                {nameof(PackageOperation.IsListed), typeof(bool)},
                {nameof(PackageOperation.Action), typeof(string)}
            };

        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            if (!(value is PackageOperation operation)) throw new NotSupportedException("A PackageOperation is required.");

            var bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                {"data", value},
                {nameof(PackageOperation.Id), operation.Id},
                {nameof(PackageOperation.Version), operation.Version},
                {nameof(PackageOperation.VersionVerbatim), operation.VersionVerbatim},
                {nameof(PackageOperation.VersionNormalized), operation.VersionNormalized},
                {nameof(PackageOperation.Published), operation.Published},
                {nameof(PackageOperation.PackageUrl), operation.PackageUrl},
                {nameof(PackageOperation.IsListed), operation.IsListed},
                {nameof(PackageOperation.Action), operation.Action}
            };

            var argument = _parameter.ParameterType == typeof(string)
                ? JsonConvert.SerializeObject(value, Formatting.Indented)
                : value;

            IValueProvider valueBinder = new PackageOperationValueProvider(_parameter, argument);
            return Task.FromResult<ITriggerData>(new TriggerData(valueBinder, bindingData));
        }

        public ParameterDescriptor ToParameterDescriptor() =>
            new TriggerParameterDescriptor
            {
                Name = _parameter.Name
            };

        internal class PackageOperationValueProvider : IValueProvider
        {
            private readonly ParameterInfo _parameter;
            private readonly object _value;

            public PackageOperationValueProvider(ParameterInfo parameter, object value)
            {
                _parameter = parameter;
                _value = value;
            }

            public Type Type => _parameter.ParameterType;

            public Task<object> GetValueAsync() => Task.FromResult(_value);

            public string ToInvokeString()
            {
                // TODO return a nicer dashboard string
                return $"{_value}";
            }
        }
    }
}