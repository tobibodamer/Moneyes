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

        IReadOnlyList<T> FindAllById(params object[] ids);

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
        int CreateMany(IEnumerable<T> entities);

        /// <summary>
        /// Inserts the given <paramref name="entities"/> into the database.
        /// </summary>
        /// <param name="entities">The entities to insert.</param>
        /// <param name="onConflict">A delegate that is invoked when a constraint violation occurs.</param>
        /// <returns>The number of inserted entities.</returns>
        int CreateMany(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict);

        /// <summary>
        /// Creates or updates a given entity.
        /// </summary>
        /// <param name="entity">The entity to create / update.</param>
        /// <param name="onConflict">A delegate that is invoked when a constraint violation occurs.</param>
        /// <returns><see langword="true"/> if the entity was created, otherwise <see langword="false"/>.</returns>
        bool Set(T entity, ConflictResolutionDelegate<T> onConflict);

        /// <summary>
        /// Inserts an entity if the given <paramref name="id"/> does not exist,
        /// or updates the existing entity, using the provided add / update functions.
        /// </summary>
        /// <param name="id">The id of the entity to create / update.</param>
        /// <param name="addEntityFactory">The function used to create an entity if the given key is not present.</param>
        /// <param name="updateEntityFactory">The function used to create an entity for an existing key and entity.</param>
        /// <param name="onConflict">A delegate that is invoked when a constraint violation occurs.</param>
        /// <returns><see langword="true"/> if the entity was created, otherwise <see langword="false"/>.</returns>
        bool Set(object id, Func<object, T> addEntityFactory, Func<object, T, T> updateEntityFactory, ConflictResolutionDelegate<T> onConflict = null);

        /// <summary>
        /// Inserts multiple entities if they dont exist and updates existing entities.
        /// </summary>
        /// <param name="entities">The entities to create / update.</param>
        /// <param name="onConflict">A delegate that is invoked when a constraint violation occurs.</param>
        /// <returns>The number of inserted entities.</returns>
        int SetMany(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict);

        /// <summary>
        /// Inserts multiple entities if the id does not exist,
        /// or updates the existing entities, using the provided add / update functions.
        /// </summary>
        /// <param name="ids">The ids of the entities to create / update.</param>
        /// <param name="addEntityFactory">The function used to create an entity if the given key is not present.</param>
        /// <param name="updateEntityFactory">The function used to create an entity for an existing key and entity.</param>
        /// <param name="onConflict">A delegate that is invoked when a constraint violation occurs.</param>
        /// <returns>The number of inserted entities.</returns>
        int SetMany(IEnumerable<object> ids, Func<object, T> addEntityFactory, Func<object, T, T> updateEntityFactory, ConflictResolutionDelegate<T> onConflict = null);


        /// <summary>
        /// Updates an existing entity with a given <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The new entity to update.</param>
        /// <param name="onConflict">The conflict resolution handler to use, when a constraint violation occurs.</param>
        /// <returns><see langword="true"/> if the entity was successfully updated.</returns>
        bool Update(T entity, ConflictResolutionDelegate<T> onConflict);

        /// <summary>
        /// Updates the entity with the given <paramref name="id"/> using the <paramref name="updateEntityFactory"/>.
        /// </summary>
        /// <param name="id">The key of the entity to update.</param>
        /// <param name="updateEntityFactory">A factory function that receives the key and existing entity
        /// and returns the entity to insert.</param>
        /// <param name="onConflict">The conflict resolution handler to use, when a constraint violation occurs.</param>
        /// <returns><see langword="true"/> if the entity was successfully updated.</returns>
        bool Update(object id, Func<object, T, T> updateEntityFactory, ConflictResolutionDelegate<T> onConflict = null);

        /// <summary>
        /// Updates many existing entites.
        /// </summary>
        /// <param name="entities">The entities to update.</param>
        /// <param name="onConflict">The conflict resolution handler to use, when a constraint violation occurs.</param>
        /// <returns>The number of updated entities.</returns>
        int UpdateMany(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict);

        /// <summary>
        /// Updates multiple entities given by their id, using the provided add / update functions.
        /// </summary>
        /// <param name="ids">The ids of the entities to update.</param>
        /// <param name="updateEntityFactory">A factory function that receives the key and existing entity
        /// and returns the entity to insert.</param>
        /// <param name="onConflict">The conflict resolution handler to use, when a constraint violation occurs.</param>
        /// <returns>The number of updated entities.</returns>
        int UpdateMany(IEnumerable<object> ids, Func<object, T, T> updateEntityFactory, ConflictResolutionDelegate<T> onConflict = null);

        bool Delete(T entity);

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
        
        bool DeleteById(TKey id);

        new TKey GetKey(T entity);
    }
}
