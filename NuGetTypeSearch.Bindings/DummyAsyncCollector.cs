using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace NuGetTypeSearch.Bindings
{
    public class DummyAsyncCollector
    {
        public static IAsyncCollector<T> For<T>()
        {
            return new DummyAsyncCollectorImplementation<T>();
        }

        public class DummyAsyncCollectorImplementation<T> : IAsyncCollector<T>
        {
            public Task AddAsync(T item, CancellationToken cancellationToken = new CancellationToken()) => Task.CompletedTask;

            public Task FlushAsync(CancellationToken cancellationToken = new CancellationToken()) => Task.CompletedTask;
        }
    }
}