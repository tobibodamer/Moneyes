using System;
using System.Collections.Generic;

namespace Moneyes.Data
{
    /// <summary>
    /// Provides methods for a repository with built in cache and entities of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICachedRepository<T> : IBaseRepository<T>
    {
        /// <summary>
        /// Gets the name of the underlying table.
        /// </summary>
        string CollectionName { get; }

        /// <summary>
        /// Gets the primary key of the given <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        object GetKey(T entity);

        /// <summary>
        /// Renews the cache completely.
        /// </summary>
        void RenewCache();

        /// <summary>
        /// Renews the cache for multiple <paramref name="entities"/>.
        /// </summary>
        /// <param name="entities"></param>
        void RenewCacheFor(IEnumerable<T> entities);
        bool Set(T entity, Func<CachedRepository<T>.ConstraintViolation, ConflictResolutionAction> onConflict);
        int Set(IEnumerable<T> entities, Func<CachedRepository<T>.ConstraintViolation, ConflictResolutionAction> onConflict);

        /// <summary>
        /// Raised when the repository changed.
        /// </summary>
        event Action<RepositoryChangedEventArgs<T>> RepositoryChanged;
    }

    /// <summary>
    /// Represents a repository with built in cache, entities of type <typeparamref name="T"/> 
    /// and strongly typed primary key of type <typeparamref name="TKey"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface ICachedRepository<T, TKey> : ICachedRepository<T> 
        where TKey : struct
    {
#nullable enable
        T? FindById(TKey id);
#nullable disable

        bool Delete(T entity);
        bool DeleteById(TKey id);

        new TKey GetKey(T entity);
    }
}
