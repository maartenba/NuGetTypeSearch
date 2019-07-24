using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NuGetTypeSearch.Web
{
    public class NuGetPackageMetadataClient
    {
        private static readonly NullLogger Logger = new NullLogger();
        private static readonly NullSourceCacheContext CacheContext = new NullSourceCacheContext();

        private AutoCompleteResource _autocompleteResource;
        private PackageMetadataResource _packageMetadataResource;

        private async Task<(AutoCompleteResource, PackageMetadataResource)> InitializeResources()
        {
            var providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());

            var packageSource = new PackageSource("https://api.nuget.org/v3/index.json");

            var repository = new SourceRepository(packageSource, providers);

            return (await repository.GetResourceAsync<AutoCompleteResource>(),
                await repository.GetResourceAsync<PackageMetadataResource>());
        }

        public async Task<(IPackageSearchMetadata, bool, bool)> GetMetadataAsync(string packageId, string packageVersion)
        {
            // Ensure initialized
            if (_autocompleteResource == null || _packageMetadataResource == null)
            {
                (_autocompleteResource, _packageMetadataResource) = await InitializeResources();
            }

            var packagesMetadata = (await _packageMetadataResource.GetMetadataAsync(
                packageId, true, false, CacheContext, Logger, CancellationToken.None)).ToList();

            var version = NuGetVersion.Parse(packageVersion);

            var packageMetadata = packagesMetadata.FirstOrDefault(md => md.Identity.Version == version);
            if (packageMetadata == null)
            {
                return (null, false, false);
            }

            var isLastPreRelease = packageMetadata.Identity.Version.IsPrerelease && packagesMetadata
                                       .Where(v => v.Identity.Version.IsPrerelease)
                                       .OrderByDescending(v => v.Identity.Version)
                                       .First().Identity.Version == packageMetadata.Identity.Version;

            var isLastRelease = !packageMetadata.Identity.Version.IsPrerelease && packagesMetadata
                                       .Where(v => !v.Identity.Version.IsPrerelease)
                                       .OrderByDescending(v => v.Identity.Version)
                                       .First().Identity.Version == packageMetadata.Identity.Version;

            return (packageMetadata, isLastPreRelease, isLastRelease);
        }
    }
}