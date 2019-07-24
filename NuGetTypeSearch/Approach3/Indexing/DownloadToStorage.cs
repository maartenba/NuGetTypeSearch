using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using NuGetTypeSearch.Bindings.Catalog;

namespace NuGetTypeSearch.Approach3.Indexing
{
    public static class DownloadToStorage
    {
        private static readonly HttpClient HttpClient = SharedHttpClient.Instance;

        static DownloadToStorage()
        {
            HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(Constants.UserAgentName, Constants.UserAgentVersion));
        }

        [Disable("DISABLE-APPROACH3")]
        [FunctionName("Approach3-02-DownloadToStorage")]
        public static async Task RunAsync(
            [QueueTrigger(Constants.DownloadingQueue, Connection = Constants.DownloadingQueueConnection)] PackageOperation packageOperation,
            [Blob("packages/{Id}/{VersionNormalized}/{Id}.{VersionNormalized}.nupkg", FileAccess.ReadWrite, Connection = Constants.DownloadsConnection)] CloudBlockBlob packageBlob,
            ILogger log)
        {
            if (packageOperation.IsAdd())
            {
                await RunAddPackageAsync(packageOperation, packageBlob, log);
            }
            else if (packageOperation.IsDelete())
            {
                await RunDeletePackageAsync(packageOperation, packageBlob, log);
            }
        }

        private static async Task RunAddPackageAsync(PackageOperation packageOperation, CloudBlockBlob packageBlob, ILogger log)
        {
            log.LogInformation("Downloading package {packageId}@{packageVersionNormalized} to blob storage...", packageOperation.Id, packageOperation.VersionNormalized);

            // ReSharper disable once RedundantLogicalConditionalExpressionOperand
            if (!Constants.DevAllowOverwriteDownloadedPackage && await packageBlob.ExistsAsync())
            {
                log.LogWarning("Skip downloading package {packageId}@{packageVersionNormalized} to blob storage - package already exists.", packageOperation.Id, packageOperation.VersionNormalized);

                return;
            }

            using (var packageInputStream = await HttpClient.GetStreamAsync(packageOperation.PackageUrl))
            {
                await packageBlob.UploadFromStreamAsync(packageInputStream);
            }

            log.LogInformation("Finished downloading package {packageId}@{packageVersionNormalized} to blob storage.", packageOperation.Id, packageOperation.VersionNormalized);
        }

        private static async Task RunDeletePackageAsync(PackageOperation packageOperation, CloudBlockBlob packageBlob, ILogger log)
        {
            log.LogInformation("Deleting package {packageId}@{packageVersionNormalized} from blob storage...", packageOperation.Id, packageOperation.VersionNormalized);

            await packageBlob.DeleteIfExistsAsync();

            log.LogInformation("Deleted package {packageId}@{packageVersionNormalized} from blob storage.", packageOperation.Id, packageOperation.VersionNormalized);
        }
    }
}
