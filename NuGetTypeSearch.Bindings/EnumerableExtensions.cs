using System.Collections.Generic;
using System.Linq;

namespace NuGetTypeSearch.Bindings
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Paged<T>(this IEnumerable<T> enumerable, int pagesize)
        {
            var items = enumerable as T[] ?? enumerable.ToArray();
            var pageCount = (items.Length + pagesize - 1) / pagesize;

            var pages = Enumerable.Range(0, pageCount)
                .Select(index => items.Skip(index * pagesize).Take(pagesize));

            foreach (var page in pages)
            {
                yield return page;
            }
        }
    }
}