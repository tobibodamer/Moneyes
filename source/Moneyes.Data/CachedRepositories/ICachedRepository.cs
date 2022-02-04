﻿using System;
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
        /// Gets or sets the default conflict resolution delegate to use if none is supplied.
        /// </summary>
        Func<RepositoryOperation, ConflictResolutionDelegate<T>> DefaultConflictHandler { get; set; }

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

        bool Set(T entity, ConflictResolutionDelegate<T> onConflict);

        int Set(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict);

        /// <summary>
        /// Checks if the repostory contains any entities with the given <paramref name="ids"/>.
        /// </summary>
        /// <param name="ids">The entity keys.</param>
        /// <returns><see langword="true"/> if at least one entity exists.</returns>
        bool ContainsAny(params object[] ids);

        /// <summary>
        /// Checks if the repostory contains all entities with the given <paramref name="ids"/>.
        /// </summary>
        /// <param name="ids">The entity keys.</param>
        /// <returns><see langword="true"/> if all entities exist.</returns>
        bool ContainsAll(params object[] ids);

        /// <summary>
        /// Checks if the repostory contains an entity with the given <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The key of the entity.</param>
        /// <returns><see langword="true"/> if the entity exists.</returns>
        bool Contains(object id);

        /// <summary>
        /// Inserts the given <paramref name="entities"/> into the database.
        /// </summary>
        /// <param name="entities">The entities to insert.</param>
        /// <returns>The number of inserted entities.</returns>
        int Create(IEnumerable<T> entities);

        /// <summary>
        /// Inserts the given <paramref name="entities"/> into the database.
        /// </summary>
        /// <param name="entities">The entities to insert.</param>
        /// <param name="onConflict">A delegate that is invoked when a constraint violation occurs.</param>
        /// <returns>The number of inserted entities.</returns>
        int Create(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict);
        bool Set(object id, Func<object, T> addEntityFactory, Func<object, T, T> updateEntityFactory, ConflictResolutionDelegate<T> onConflict = null);
        int Set(IEnumerable<object> ids, Func<object, T> addEntityFactory, Func<object, T, T> updateEntityFactory, ConflictResolutionDelegate<T> onConflict = null);

        /// <summary>
        /// Updates the entity with the given <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The key of the entity to update.</param>
        /// <param name="updateEntityFactory">A factory function that receives the existing entity and key
        /// and returns the entity to insert.</param>
        /// <returns></returns>
        bool Update(object id, Func<object, T, T> updateEntityFactory, ConflictResolutionDelegate<T> onConflict = null);
        int Update(IEnumerable<object> ids, Func<object, T, T> updateEntityFactory, ConflictResolutionDelegate<T> onConflict = null);
        int Update(IEnumerable<T> entities, Func<T, T, T> updateEntityFactory, ConflictResolutionDelegate<T> onConflict = null);
        int Set(IEnumerable<T> entities, Func<T, T> addEntityFactory, Func<T, T, T> updateEntityFactory, ConflictResolutionDelegate<T> onConflict = null);
        bool Set(T entity, Func<T, T> addEntityFactory, Func<T, T, T> updateEntityFactory, ConflictResolutionDelegate<T> onConflict = null);
        bool Update(T entity, Func<T, T, T> updateEntityFactory, ConflictResolutionDelegate<T> onConflict = null);
        bool Update(T entity, ConflictResolutionDelegate<T> onConflict);
        int Update(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict);

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

        IReadOnlyList<T> FindAllById(params object[] ids);

        bool Delete(T entity);
        bool DeleteById(TKey id);

        new TKey GetKey(T entity);
    }
}
