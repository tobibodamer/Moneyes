using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace Moneyes.Data
{
    public class CachedRepository<T, TKey> : CachedRepository<T>, ICachedRepository<T, TKey>
        where TKey : struct
    {
        private readonly Func<T, TKey> _keySelector;

        public CachedRepository(
            IDatabaseProvider<ILiteDatabase> databaseProvider,
            Func<T, TKey> keySelector,
            CachedRepositoryOptions options,
            DependencyRefreshHandler refreshHandler,
            bool autoId = false,
            IEnumerable<IRepositoryDependency<T>> repositoryDependencies = null,
            IEnumerable<IUniqueConstraint<T>> uniqueConstraints = null,
            ILogger<CachedRepository<T, TKey>> logger = null)
            : base(databaseProvider, options, refreshHandler, null, autoId, repositoryDependencies, uniqueConstraints, logger)
        {
            ArgumentNullException.ThrowIfNull(keySelector);

            _keySelector = keySelector;
        }

        public virtual bool DeleteById(TKey id)
        {
            return base.DeleteById(id);
        }

        public virtual T? FindById(TKey id)
        {
            return base.FindById(id);
        }

        public override object GetKey(T entity)
        {
            return _keySelector(entity);
        }

        TKey ICachedRepository<T, TKey>.GetKey(T entity)
        {
            return _keySelector(entity);
        }
    }
    public class CachedRepository<T> : ICachedRepository<T>
    {
        private readonly Lazy<ILiteCollection<T>> _collectionLazy;
        private readonly Func<T, object> _keySelector;
        private readonly ReaderWriterLock _cacheLock = new();
        public bool IsAutoId { get; }
        public string CollectionName => Options.CollectionName;
        protected ILiteDatabase Database { get; }
        protected ILiteCollection<T> Collection => _collectionLazy.Value;
        protected IEnumerable<IRepositoryDependency<T>> RepositoryDependencies { get; set; }
        protected IEnumerable<IUniqueConstraint<T>> UniqueConstraints { get; set; }
        protected CachedRepositoryOptions Options { get; }
        protected DependencyRefreshHandler DependencyRefreshHandler { get; }
        protected Dictionary<object, T> Cache { get; } = new();

        protected ILogger<CachedRepository<T>> Logger { get; }

        public event Action<T> EntityAdded;
        public event Action<T> EntityUpdated;
        public event Action<T> EntityDeleted;
        public event Action<RepositoryChangedEventArgs<T>> RepositoryChanged;

        private const int CacheTimeout = 2000;
        public CachedRepository(
            IDatabaseProvider<ILiteDatabase> databaseProvider,
            CachedRepositoryOptions options,
            DependencyRefreshHandler refreshHandler,
            Func<T, object> keySelector = null,
            bool autoId = false,
            IEnumerable<IRepositoryDependency<T>> repositoryDependencies = null,
            IEnumerable<IUniqueConstraint<T>> uniqueConstraints = null,
            ILogger<CachedRepository<T>> logger = null)
        {
            ArgumentNullException.ThrowIfNull(databaseProvider);

            _keySelector = keySelector;
            RepositoryDependencies = repositoryDependencies;
            UniqueConstraints = uniqueConstraints;
            Database = databaseProvider.Database;
            Options = options;
            DependencyRefreshHandler = refreshHandler;
            Logger = logger;
            _collectionLazy = new Lazy<ILiteCollection<T>>(CreateCollection);

            SetupDependencies();
        }

        #region Setup

        protected virtual ILiteCollection<T> CreateCollection()
        {
            var collection = Database.GetCollection<T>(Options.CollectionName);

            // Apply includes of dependencies to collection
            foreach (var dependency in RepositoryDependencies)
            {
                collection = dependency.Apply(collection);
            }

            return collection;
        }

        /// <summary>
        /// Registers callbacks for the <see cref="RepositoryDependencies"/>.
        /// </summary>
        protected virtual void SetupDependencies()
        {
            foreach (var dependency in RepositoryDependencies)
            {
                DependencyRefreshHandler.RegisterCallback(dependency, OnDependencyChanged);
            }
        }

        /// <summary>
        /// This method is called when a dependency entity changed.
        /// </summary>
        /// <param name="dependency">The associated repository dependency.</param>
        /// <param name="e">The change args.</param>
        protected virtual void OnDependencyChanged(IRepositoryDependency<T> dependency, DependencyRefreshHandler.DepedencyChangedEventArgs e)
        {
            var affectedEntities = GetFromCache()
                   .Where(x => dependency.NeedsRefresh(e.ChangedKey, x))
                   .ToList();

            Logger.LogInformation("OnDependencyChanged() called with {@dependency} and {@args}",
                new { Source = dependency.SourceCollectionName, Target = dependency.TargetCollectionName },
                new { Key = e.ChangedKey, e.Action });
            Logger.LogInformation("Affected entities: {@entities}", affectedEntities.Count);

            if (!affectedEntities.Any())
            {
                return;
            }

            // Replace dependent properties with updated value
            foreach (var entity in affectedEntities)
            {
                dependency.UpdateDependency(entity, e);
            }

            // NOTE: Cache doesn't need update because references are changed

            // Forward changes to next dependencies
            foreach (var entity in affectedEntities)
            {
                DependencyRefreshHandler.OnChangesMade(this, entity, e.Action);
            }
        }

        #endregion


        #region Cache

        /// <summary>
        /// A transform method that is applied post query, before updating the cache.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected virtual T PostQueryTransform(T entity) => entity;
        public void RenewCache()
        {
            _cacheLock.AcquireWriterLock(CacheTimeout);

            List<T> entitiesToUpdate;

            try
            {
                entitiesToUpdate = Collection.FindAll().Select(PostQueryTransform).ToList();
            }
            catch
            {
                _cacheLock.ReleaseWriterLock();
                throw;
            }

            RefreshCacheForInternal(entitiesToUpdate, true);
        }
        public void RenewCacheFor(IEnumerable<T> entities)
        {
            _cacheLock.AcquireWriterLock(CacheTimeout);

            List<T> entitiesToUpdate;

            try
            {
                var keys = entities.Select(GetKey).Select(x => new BsonValue(x));

                Logger.LogInformation("Reloading {count} entities...", keys.Count());

                entitiesToUpdate = Collection.Find(Query.In("_id", keys)).Select(PostQueryTransform).ToList();

                Logger.LogInformation("{count} entities loaded", entitiesToUpdate.Count);
            }
            catch
            {
                _cacheLock.ReleaseWriterLock();
                throw;
            }

            RefreshCacheForInternal(entitiesToUpdate, true);
        }

        private void RefreshCacheForInternal(IEnumerable<T> entities, bool isWriteLockHeld)
        {
            try
            {
                List<T> entitiesToUpdate = entities.ToList();

                Logger.LogInformation("Updating cache for {count} entities", entitiesToUpdate.Count);

                if (!isWriteLockHeld)
                {
                    _cacheLock.AcquireWriterLock(CacheTimeout);
                }

                foreach (T entity in entitiesToUpdate)
                {
                    AddOrUpdateCache(entity);
                }

                Logger.LogInformation("Cache updated.");
            }
            finally
            {
                _cacheLock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Updates the cache with the given entities, while holding a write lock.
        /// </summary>
        /// <param name="entities"></param>
        protected void UpdateCacheLocked(IEnumerable<T> entities)
        {
            _cacheLock.AcquireWriterLock(CacheTimeout);
            try
            {
                foreach (var entity in entities)
                {
                    AddOrUpdateCache(entity);
                }
            }
            finally
            {
                _cacheLock.ReleaseWriterLock();
            }
        }

        protected void AddOrUpdateCache(T entity)
        {
            object key = GetKey(entity);

            Cache[key] = entity;
        }
        protected bool AddToCache(T entity)
        {
            object key = GetKey(entity);

            return Cache.TryAdd(key, entity);
        }
        protected IReadOnlyList<T> GetFromCache()
        {
            List<T> entities;

            _cacheLock.AcquireReaderLock(CacheTimeout);

            try
            {
                entities = Cache.Values.ToList();
            }
            finally
            {
                _cacheLock.ReleaseReaderLock();
            }

            return entities;
        }

        #endregion

        #region Validation
        protected virtual bool ValidateUniqueConstaintsFor(T entity)
        {
            var existingEntities = GetFromCache();

            foreach (var constraint in UniqueConstraints)
            {
                foreach (var existingEntity in existingEntities)
                {
                    if (!constraint.Allows(entity, existingEntity))
                    {
                        //constraint.PropertyName
                        //TODO: Log
                        return false;
                    }
                }
            }

            return false;
        }
        protected virtual bool ValidateUniqueConstaintsFor(IEnumerable<T> entities)
        {
            var existingEntities = GetFromCache();
            var entitiesList = entities.ToList();

            // Combine all constraints with all entities with all existing entities
            var zipped = UniqueConstraints
                .Zip(entitiesList, existingEntities);

            // Validate every combination
            foreach (var (constraint, entity, existingEntity) in zipped)
                if (!constraint.Allows(entity, existingEntity))
                {
                    Logger.LogWarning("Unique constraint violation of '{Property}' (Entity ID: {id})",
                        constraint.PropertyName, GetKey(entity));

                    return false;
                }

            return false;
        }

        protected virtual bool ValidatePrimaryKey(object key)
        {
            var existingEntities = GetFromCache();

            foreach (var existingEntity in existingEntities)
            {
                if (GetKey(existingEntity).Equals(key))
                {
                    Logger.LogWarning("Primary key violation: {key} already exists", key);
                    return false;
                }
            }

            return false;
        }

        #endregion

        public virtual object GetKey(T entity)
        {
            return _keySelector?.Invoke(entity) ?? null;
        }

        #region CRUD
        public virtual T Create(T entity)
        {
            object key;

            if (!IsAutoId)
            {
                key = GetKey(entity);

                if (!ValidatePrimaryKey(key))
                {
                    throw new InvalidOperationException();
                }
            }

            if (!ValidateUniqueConstaintsFor(entity))
            {
                throw new InvalidOperationException();
            }


            var bsonKey = Collection.Insert(entity);
            var createdEntity = Collection.FindById(bsonKey);

            key = bsonKey.RawValue;

            _cacheLock.AcquireWriterLock(CacheTimeout);

            try
            {
                // Update cache with created entity
                AddOrUpdateCache(createdEntity);
            }
            finally
            {
                _cacheLock.ReleaseWriterLock();
            }

            OnEntityAdded(createdEntity);
            OnRepositoryChanged(RepositoryChangedAction.Add, addedItems: new List<T>() { createdEntity });

            return createdEntity;
        }

        public virtual IEnumerable<T> GetAll()
        {
            return GetFromCache();
        }

#nullable enable
        public virtual T? FindById(object id)
#nullable disable
        {
            ArgumentNullException.ThrowIfNull(id);

            _cacheLock.AcquireReaderLock(CacheTimeout);

            T entity;
            bool found;

            try
            {
                found = Cache.TryGetValue(id, out entity);
            }
            finally
            {
                _cacheLock.ReleaseReaderLock();
            }

            if (found)
            {
                return entity;
            }

            return default;
        }

        public virtual bool Set(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            object key;
            bool inserted = false;

            try
            {
                _cacheLock.AcquireWriterLock(CacheTimeout);

                // Upsert in db
                inserted = Collection.Upsert(entity);

                // Get key after upsert (might be not set until auto id)
                key = GetKey(entity);

                // Upsert in cache
                AddOrUpdateCache(entity);
            }
            finally
            {
                _cacheLock.ReleaseWriterLock();
            }

            if (inserted)
            {
                OnEntityAdded(entity);
                OnRepositoryChanged(RepositoryChangedAction.Add, addedItems: new List<T>() { entity });
            }
            else
            {
                OnEntityUpdated(entity);
                OnRepositoryChanged(RepositoryChangedAction.Replace, replacedItems: new List<T>() { entity });
            }

            return inserted;
        }
        public virtual int Set(IEnumerable<T> entities)
        {
            List<T> entitiesToSet = entities.ToList();

            int counter = Collection.Upsert(entitiesToSet);

            List<T> addedEntities = new();
            List<T> updatedEntities = new();

            foreach (T entity in entities)
            {
                object id = GetKey(entity);

                if (AddToCache(entity))
                {
                    addedEntities.Add(entity);
                    OnEntityAdded(entity);
                    continue;
                }

                T oldValue = Cache[id];

                // Key exists -> update
                AddOrUpdateCache(entity);

                if (!oldValue.Equals(entity))
                {
                    updatedEntities.Add(entity);
                    OnEntityUpdated(entity);
                }
            }

            OnRepositoryChanged(RepositoryChangedAction.Add | RepositoryChangedAction.Replace,
                addedItems: addedEntities,
                replacedItems: updatedEntities);

            return counter;
        }

        public virtual void Update(T entity)
        {
            object key = GetKey(entity);

            ArgumentNullException.ThrowIfNull(key);

            try
            {
                _cacheLock.AcquireReaderLock(CacheTimeout);

                if (!Cache.ContainsKey(key))
                {
                    throw new KeyNotFoundException();
                }
            }
            catch
            {
                _cacheLock.ReleaseReaderLock();
                throw;
            }

            bool inserted = false;

            try
            {
                _cacheLock.UpgradeToWriterLock(CacheTimeout);

                // Upsert in db
                inserted = Collection.Update(new BsonValue(key), entity);

                // Get key after update (might be not set until auto id)
                key = GetKey(entity);

                // Upsert in cache
                AddOrUpdateCache(entity);
            }
            finally
            {
                _cacheLock.ReleaseWriterLock();
            }

            if (inserted)
            {
                OnEntityUpdated(entity);
                OnRepositoryChanged(RepositoryChangedAction.Replace, replacedItems: new List<T>() { entity });
            }
        }

        public virtual int Update(IEnumerable<T> entities)
        {
            List<T> entitiesList = entities.ToList();
            var keys = entitiesList.Select(GetKey).ToHashSet();

            var entitiesToUpdate = entitiesList.Where(e => keys.Contains(GetKey(e))).ToList();

            //TODO: Validate Unique Constraints

            int counter = Collection.Update(entitiesList);

            foreach (T entity in entitiesToUpdate)
            {
                OnEntityUpdated(entity);
            }

            OnRepositoryChanged(RepositoryChangedAction.Replace,
                replacedItems: entitiesToUpdate);

            return counter;
        }

        public virtual bool Delete(T entity)
        {
            object key = GetKey(entity);

            return DeleteById(key);
        }
        public virtual bool DeleteById(object id)
        {
            ArgumentNullException.ThrowIfNull(id);

            _cacheLock.AcquireWriterLock(CacheTimeout);

            bool deleted;
            try
            {
                deleted = Collection.Delete(new BsonValue(id));
            }
            catch
            {
                _cacheLock.ReleaseWriterLock();
                throw;
            }

            if (deleted)
            {
                T deletedEntity;

                try
                {
                    // Remove deleted entity from cache
                    _ = Cache.Remove(id, out deletedEntity);
                }
                finally
                {
                    _cacheLock.ReleaseWriterLock();
                }

                OnEntityDeleted(deletedEntity);
                OnRepositoryChanged(RepositoryChangedAction.Remove, replacedItems: new List<T>() { deletedEntity });

                return true;
            }

            _cacheLock.ReleaseWriterLock();

            return false;
        }
        public virtual int DeleteMany(Expression<Func<T, bool>> predicate)
        {
            int counter = Collection.DeleteMany(predicate);

            List<T> removedEntities = new();

            foreach (var kvp in Cache.Where(kvp => predicate.Compile()(kvp.Value)))
            {
                object id = kvp.Key;

                if (Cache.Remove(id, out var entity))
                {
                    removedEntities.Add(entity);
                    OnEntityDeleted(entity);
                    continue;
                }
            }

            OnRepositoryChanged(RepositoryChangedAction.Remove, removedItems: removedEntities);

            return counter;
        }
        public virtual int DeleteAll()
        {
            int counter;

            try
            {
                _cacheLock.AcquireReaderLock(CacheTimeout);

                counter = Collection.DeleteAll();
            }
            catch
            {
                _cacheLock.ReleaseReaderLock();
                throw;
            }

            try
            {
                _cacheLock.UpgradeToWriterLock(CacheTimeout);

                Cache.Clear();

                return counter;
            }
            finally
            {
                _cacheLock.ReleaseWriterLock();
            }
        }

        #endregion

        #region Events
        protected virtual void OnEntityUpdated(T entity, bool notifyDependencyHandler = true)
        {
            if (notifyDependencyHandler)
            {
                DependencyRefreshHandler.OnChangesMade(this, entity, RepositoryChangedAction.Replace);
            }
            EntityUpdated?.Invoke(entity);
        }

        protected virtual void OnEntityAdded(T entity, bool notifyDependencyHandler = true)
        {
            if (notifyDependencyHandler)
            {
                DependencyRefreshHandler.OnChangesMade(this, entity, RepositoryChangedAction.Add);
            }
            EntityAdded?.Invoke(entity);
        }

        protected virtual void OnEntityDeleted(T entity, bool notifyDependencyHandler = true)
        {
            if (notifyDependencyHandler)
            {
                DependencyRefreshHandler.OnChangesMade(this, entity, RepositoryChangedAction.Remove);
            }
            EntityDeleted?.Invoke(entity);
        }

        protected virtual void OnRepositoryChanged(
            RepositoryChangedAction actions,
            IReadOnlyList<T> addedItems = null,
            IReadOnlyList<T> replacedItems = null,
            IReadOnlyList<T> removedItems = null)
        {
            var args = new RepositoryChangedEventArgs<T>(actions)
            {
                AddedItems = addedItems,
                ReplacedItems = replacedItems,
                RemovedItems = removedItems
            };

            RepositoryChanged?.Invoke(args);
        }

        #endregion
    }
}
