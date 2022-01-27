using System;
using Moneyes.Core;

namespace Moneyes.Data
{
    public static class UniqueCachedRepositoryExtensions
    {
        public static KeyCachedRepositoryBuilder<T> AddUniqueRepository<T>(
            this CachedRepositoriesOptions options, Action<CachedRepositoryOptions> configure = null) 
            where T : UniqueEntity
        {
            return options
                .AddRepository<T, IUniqueCachedRepository<T>>(configure)
                .UseFactory<UniqueCachedRepositoryFactory<T>>()
                .HasKey(x => x.Id)
                .As<IUniqueCachedRepository<T>>();
        }

        public static KeyCachedRepositoryBuilder<T> AddUniqueRepository<T>(
            this CachedRepositoriesOptions options, string collectionName) 
            where T : UniqueEntity
        {
            return options
                .AddRepository<T, IUniqueCachedRepository<T>>(collectionName)
                .UseFactory<UniqueCachedRepositoryFactory<T>>()
                .HasKey(x => x.Id)
                .As<IUniqueCachedRepository<T>>();
        }
    }
}
