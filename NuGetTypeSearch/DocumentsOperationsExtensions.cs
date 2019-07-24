using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace NuGetTypeSearch
{
    public static class DocumentsOperationsExtensions
    {
        public static async Task ForEachAsync<T>(
            this IDocumentsOperations operations,
            string searchText,
            Func<SearchResult<T>, bool> action,
            SearchParameters searchParameters = null,
            SearchRequestOptions searchRequestOptions = null,
            CancellationToken cancellationToken = default)
            where T : class
        {
            var documentSearchResult = await operations.SearchAsync<T>(searchText, searchParameters, searchRequestOptions, cancellationToken);
            
            foreach (var searchResult in documentSearchResult.Results)
            {
                if (!action(searchResult)) return;
            }
            
            while (documentSearchResult.ContinuationToken != null)
            {
                documentSearchResult = await operations.ContinueSearchAsync<T>(documentSearchResult.ContinuationToken, cancellationToken: cancellationToken);

                foreach (var searchResult in documentSearchResult.Results)
                {
                    if (!action(searchResult)) return;
                }
            }
        }
    }
}