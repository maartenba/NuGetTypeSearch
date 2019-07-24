namespace NuGetTypeSearch
{
    public static class Constants
    {
        // Set these to their inverse for production!
        public const bool DevAllowOverwriteDownloadedPackage = false;
        public const bool DevCommitToSearchIndex = false;

        public const string IndexingQueue = "indexingqueue";
        public const string IndexingQueueConnection = "AzureWebJobsStorage";

        public const string SearchServiceName = "nugettypesearch";
        public const string SearchServiceKey = "XXXXXXXXXXXXXXXXXXXXXXXX";
        public const string SearchServiceIndexName = "packages";
        public const string IndexConnection = "AzureWebJobsStorage";

        public const string DownloadingQueue = "downloadingqueue";
        public const string DownloadingQueueConnection = "AzureWebJobsStorage";
        public const string DownloadsConnection = "AzureWebJobsStorage";

        public const string UserAgentName = "NuGetTypeSearch";
        public const string UserAgentVersion = "1.0.0";

        //public const string NuGetPackageUrlTemplate = "https://api.nuget.org/v3/flatcontainer/{0}/{1}/{0}.{1}.nupkg";
        public const string NuGetPackageUrlTemplate = "https://az320820.vo.msecnd.net/packages/{0}.{1}.nupkg";
        public const string NuGetPackageDefaultIconUrl = "https://www.nuget.org/Content/gallery/img/default-package-icon-256x256.png";
    }
}