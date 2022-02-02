using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Moneyes.Data
{
    internal class RepositoryProvider : IRepositoryProvider
    {
        private readonly IServiceProvider _serviceProvider;
        public RepositoryProvider(IServiceProvider p)
        {
            _serviceProvider = p;
        }

        public ICachedRepository<T> GetRepository<T>()
        {
            var repository = _serviceProvider.GetService<ICachedRepository<T>>();

            var deps = _serviceProvider.GetServices<IRepositoryDependency<T>>()
                .Where(c => c.TargetCollectionName.Equals(repository.CollectionName));

            return repository;
        }

        public ICachedRepository<T> GetRepository<T>(string collectionName = null)
        {
            if (collectionName == null)
            {
                return GetRepository<T>();
            }

            return _serviceProvider.GetServices<ICachedRepository<T>>()
                .FirstOrDefault(r => r.CollectionName.Equals(collectionName));
        }

        public ICachedRepository<T, TKey> GetRepository<T, TKey>(string collectionName = null) where TKey : struct
        {
            if (collectionName == null)
            {
                return _serviceProvider.GetService<ICachedRepository<T, TKey>>();
            }

            return _serviceProvider.GetServices<ICachedRepository<T, TKey>>()
                .FirstOrDefault(r => r.CollectionName.Equals(collectionName));
        }
    }
}
