using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using NuGet.Protocol.Catalog;

namespace NuGetTypeSearch.Catalog
{
    public class CloudBlobCursor 
        : ICursor
    {
        private readonly CloudBlockBlob _cursorBlob;

        private DateTimeOffset? _lastKnownValue;

        public CloudBlobCursor(CloudBlockBlob cursorBlob)
        {
            _cursorBlob = cursorBlob;
        }

        public async Task<DateTimeOffset?> GetAsync()
        {
            if (!await _cursorBlob.ExistsAsync())
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<DateTimeOffset>(
                    await _cursorBlob.DownloadTextAsync());
            }
            catch
            {
                return _lastKnownValue;
            }
        }

        public async Task SetAsync(DateTimeOffset value)
        {
            _lastKnownValue = value;

            try
            { 
                await _cursorBlob.UploadTextAsync(
                    JsonConvert.SerializeObject(value));
            }
            catch
            {
                // intentional
            }
        }
    }
}