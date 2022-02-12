using System;
using Moneyes.Core;

namespace Moneyes.Data
{
    public static class UniqueCachedRepositoryExtensions
    {
        public static KeyCachedRepositoryBuilder<T> AddUniqueRepository<T>(
            this CachedRepositoriesBuilder options, Action<CachedRepositoryOptions> configure = null) 
            where T : UniqueEntity<T>
        {
            return options
                .AddRepository<T, IUniqueCachedRepository<T>>(configure)
                .UseFactory<UniqueCachedRepositoryFactory<T>>()
                .HasKey(x => x.Id)
                .As<IUniqueCachedRepository<T>>();
        }

        public static KeyCachedRepositoryBuilder<T> AddUniqueRepository<T>(
            this CachedRepositoriesBuilder options, string collectionName, bool preloadCache = false) 
            where T : UniqueEntity<T>
        {
            return options
                .AddRepository<T, IUniqueCachedRepository<T>>(x =>
                {
                    x.CollectionName = collectionName;
                    x.PreloadCache = preloadCache;
                })
                .UseFactory<UniqueCachedRepositoryFactory<T>>()
                .HasKey(x => x.Id)
                .As<IUniqueCachedRepository<T>>();
        }
    }
}
