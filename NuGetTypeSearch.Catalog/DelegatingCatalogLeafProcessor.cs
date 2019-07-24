using System;
using System.Threading.Tasks;
using NuGet.Protocol.Catalog;
using NuGet.Protocol.Catalog.Models;

namespace NuGetTypeSearch.Catalog
{
    public class DelegatingCatalogLeafProcessor 
        : ICatalogLeafProcessor
    {
        private readonly Func<PackageDetailsCatalogLeaf, Task<bool>> _packageAdded;
        private readonly Func<PackageDeleteCatalogLeaf, Task<bool>> _packageDeleted;

        public DelegatingCatalogLeafProcessor(
            Func<PackageDetailsCatalogLeaf, Task<bool>> packageAdded,
            Func<PackageDeleteCatalogLeaf, Task<bool>> packageDeleted)
        {
            _packageAdded = packageAdded;
            _packageDeleted = packageDeleted;
        }

        public Task<bool> ProcessPackageDetailsAsync(PackageDetailsCatalogLeaf leaf) => _packageAdded?.Invoke(leaf);

        public Task<bool> ProcessPackageDeleteAsync(PackageDeleteCatalogLeaf leaf) => _packageDeleted?.Invoke(leaf);
    }
}
