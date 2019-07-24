using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.WebJobs;

namespace NuGetTypeSearch.Bindings.Search.Bindings
{
    public class AzureSearchAsyncCollector<T> : IAsyncCollector<T>
        where T : class
    {
        private readonly AzureSearchIndexAttribute _indexAttribute;
        private readonly SearchServiceClient _searchServiceClient;
        private readonly ISearchIndexClient _indexClient;
        private readonly Func<T, IndexBatch<T>> _indexAction;

        public AzureSearchAsyncCollector(AzureSearchIndexAttribute indexAttribute)
        {
            _indexAttribute = indexAttribute;

            _searchServiceClient = new SearchServiceClient(
                _indexAttribute.SearchServiceName,
                new SearchCredentials(_indexAttribute.SearchServiceKey));

           _indexClient = _searchServiceClient.Indexes.GetClient(_indexAttribute.IndexName);

           switch (_indexAttribute.IndexAction)
           {
               case IndexAction.MergeOrUpload:
                   _indexAction = item => IndexBatch.MergeOrUpload(new[] { item });
                   break;
               case IndexAction.Merge:
                   _indexAction = item => IndexBatch.Merge(new[] { item });
                    break;
               case IndexAction.Upload:
                   _indexAction = item => IndexBatch.Upload(new[] { item });
                    break;
               case IndexAction.Delete:
                   _indexAction = item => IndexBatch.Delete(new[] { item });
                    break;
               default:
                   throw new ArgumentOutOfRangeException();
           }
        }

        public async Task AddAsync(T item, CancellationToken cancellationToken = new CancellationToken())
        {
            async Task IndexItemAsync()
            {
                await _indexClient.Documents.IndexAsync(
                    _indexAction(item),
                    cancellationToken: cancellationToken);
            }

            try
            {
                await IndexItemAsync();
            }
            catch (Exception e) when(e.Message.Contains("index") && e.Message.Contains("was not found"))
            {
                if (_indexAttribute.CreateOrUpdateIndex)
                {
                    await CreateIndex();
                    await IndexItemAsync();
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task CreateIndex()
        {
            IList<Field> fieldInfo = null;
            if (_indexAttribute.IndexDocumentType != null)
            {
                // IndexDocumentType specified? Use that.
                var methodInfo = typeof(FieldBuilder).GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .First(m => m.Name == nameof(FieldBuilder.BuildForType) && m.GetParameters().Length == 0);
                var genericMethodInfo = methodInfo.MakeGenericMethod(_indexAttribute.IndexDocumentType);
                fieldInfo = genericMethodInfo.Invoke(null, null) as IList<Field>;
            }
            else
            {
                // Use raw type
                fieldInfo = FieldBuilder.BuildForType<T>();
            }

            await _searchServiceClient.Indexes.CreateOrUpdateAsync(new Index
            {
                Name = _indexAttribute.IndexName,
                Fields = fieldInfo
            });
        }

        public Task FlushAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.CompletedTask;
        }
    }
}