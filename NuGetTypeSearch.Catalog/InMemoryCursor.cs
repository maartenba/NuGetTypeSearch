using System;
using System.Threading.Tasks;
using NuGet.Protocol.Catalog;

namespace NuGetTypeSearch.Catalog
{
    public class InMemoryCursor 
        : ICursor
    {
        private DateTimeOffset? _fromDateTimeOffset;

        public InMemoryCursor(DateTimeOffset? fromDateTimeOffset)
        {
            _fromDateTimeOffset = fromDateTimeOffset;
        }

        public Task<DateTimeOffset?> GetAsync()
        {
            return Task.FromResult(_fromDateTimeOffset);
        }

        public Task SetAsync(DateTimeOffset value)
        {
            _fromDateTimeOffset = value;

            return Task.CompletedTask;
        }
    }
}