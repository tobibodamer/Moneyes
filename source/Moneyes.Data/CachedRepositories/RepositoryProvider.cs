using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Moneyes.Data
{
    internal class RepositoryProvider : IRepositoryProvider, IDisposable
    {
        private readonly IServiceScope _serviceScope;
        public RepositoryProvider(IServiceScope scope)
        {
            _serviceScope = scope;
        }

        public ICachedRepository<T> GetRepository<T>()
        {
            return _serviceScope.ServiceProvider.GetService<ICachedRepository<T>>();
        }

        public ICachedRepository<T> GetRepository<T>(string collectionName = null)
        {
            if (collectionName == null)
            {
                return GetRepository<T>();
            }

            return _serviceScope.ServiceProvider.GetServices<ICachedRepository<T>>()
                .FirstOrDefault(r => r.CollectionName.Equals(collectionName));
        }

        public ICachedRepository<T, TKey> GetRepository<T, TKey>(string collectionName = null) where TKey : struct
        {
            if (collectionName == null)
            {
                return _serviceScope.ServiceProvider.GetService<ICachedRepository<T, TKey>>();
            }

            return _serviceScope.ServiceProvider.GetServices<ICachedRepository<T, TKey>>()
                .FirstOrDefault(r => r.CollectionName.Equals(collectionName));
        }
        public void Dispose()
        {
            _serviceScope.Dispose();
        }
    }
}
