using System;
using System.Threading.Tasks;

namespace NuGetTypeSearch.ODataDump
{
    class Program
    {
        // ReSharper disable once UnusedParameter.Local
        static async Task Main(string[] args)
        {
            var packageProvider = new NuGetPackageProvider("https://www.nuget.org/api/v2", Console.Out);
            await packageProvider.GetPackages(DateTime.MinValue, async packages =>
            {
                foreach (var package in packages)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("[ADDED] ");
                    Console.ResetColor();
                    Console.WriteLine("{2} - {0}@{1}", package.PackageIdentifier, package.PackageVersion, package.LastEdited);
                }
            });
        }
    }
}
