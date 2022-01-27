using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Moneyes.Data
{
    public class CachedRepositoryFactory<T> : IRepositoryFactory<T>
    {
        protected IServiceProvider ServiceProvider { get; }

        public CachedRepositoryFactory(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }
        protected IEnumerable<IUniqueConstraint<T>> GetUniqueConstraints(string collectionName)
        {
            // Get all unique constraints for this collection type and name
            return ServiceProvider.GetServices<IUniqueConstraint<T>>()
                .Where(c => c.CollectionName.Equals(collectionName));
        }

        protected IEnumerable<IRepositoryDependency<T>> GetRepositoryDependencies(string collectionName)
        {
            // Get all repo dependencies for this collection type and name
            return ServiceProvider.GetServices<IRepositoryDependency<T>>()
                .Where(dep => dep.TargetCollectionName.Equals(collectionName));
        }
        public virtual ICachedRepository<T> CreateRepository(CachedRepositoryOptions options, bool autoId)
        {
            string collectionName = options.CollectionName;

            var repositoryDependencies = GetRepositoryDependencies(collectionName);
            var uniqueConstraints = GetUniqueConstraints(collectionName);
            var databaseProvider = ServiceProvider.GetRequiredService<IDatabaseProvider>();
            var refreshHandler = ServiceProvider.GetRequiredService<DependencyRefreshHandler>();

            return new CachedRepository<T>(
                databaseProvider,
                options,
                refreshHandler,
                repositoryDependencies: repositoryDependencies,
                uniqueConstraints: uniqueConstraints);
        }
    }

    public class CachedRepositoryFactory<T, TKey> : CachedRepositoryFactory<T>, IRepositoryFactory<T, TKey>
        where TKey : struct
    {        
        public CachedRepositoryFactory(IServiceProvider serviceProvider) : base(serviceProvider)
        {            
        }
        
        public virtual ICachedRepository<T, TKey> CreateRepository(CachedRepositoryOptions options, Func<T, TKey> keySelector, bool autoId) 
        {
            string collectionName = options.CollectionName;

            var repositoryDependencies = GetRepositoryDependencies(collectionName);
            var uniqueConstraints = GetUniqueConstraints(collectionName);
            var databaseProvider = ServiceProvider.GetRequiredService<IDatabaseProvider>();
            var dependencyRefreshHandler = ServiceProvider.GetService<DependencyRefreshHandler>();

            return new CachedRepository<T, TKey>(
                databaseProvider, 
                keySelector, 
                options, 
                dependencyRefreshHandler, 
                autoId, 
                repositoryDependencies, 
                uniqueConstraints);
        }
    }
}
