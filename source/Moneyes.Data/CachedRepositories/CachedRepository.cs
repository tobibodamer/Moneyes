using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;

namespace Moneyes.Data
{
    public delegate bool NeedsRefresh<T>(T entity);
    public class CachedRepository<T, TKey> : CachedRepository<T>, ICachedRepository<T, TKey>
        where TKey : struct
    {
        private readonly Func<T, TKey> _keySelector;

        public CachedRepository(
            IDatabaseProvider databaseProvider,
            Func<T, TKey> keySelector,
            CachedRepositoryOptions options,
            bool autoId = false,
            IEnumerable<IRepositoryDependency<T>> repositoryDependencies = null,
            IEnumerable<IUniqueConstraint<T>> uniqueConstraints = null)
            : base(databaseProvider, options, null, autoId, repositoryDependencies, uniqueConstraints)
        {
            ArgumentNullException.ThrowIfNull(keySelector);

            _keySelector = keySelector;
        }

        public bool Delete(TKey id)
        {
            return base.Delete(id);
        }

        public T? FindById(TKey id)
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
        protected ConcurrentDictionary<object, T> Cache { get; } = new();

        public event Action<T> EntityAdded;
        public event Action<T> EntityUpdated;
        public event Action<T> EntityDeleted;
        public event Action<RepositoryChangedEventArgs<T>> RepositoryChanged;

        private const int CacheTimeout = 2000;
        public CachedRepository(
            IDatabaseProvider databaseProvider,
            CachedRepositoryOptions options,
            Func<T, object> keySelector = null,
            bool autoId = false,
            IEnumerable<IRepositoryDependency<T>> repositoryDependencies = null,
            IEnumerable<IUniqueConstraint<T>> uniqueConstraints = null)
        {
            ArgumentNullException.ThrowIfNull(databaseProvider);

            _keySelector = keySelector;
            RepositoryDependencies = repositoryDependencies;
            UniqueConstraints = uniqueConstraints;
            Database = databaseProvider.Database;
            Options = options;

            _collectionLazy = new Lazy<ILiteCollection<T>>(CreateCollection);


            if (options.PreloadCache)
            {
                Task.Run(() =>
                {
                    try
                    {
                        RefreshCache();
                    }
                    catch (Exception ex)
                    {
                        // Update cache failed
                    }
                });
            }

            //SetupDependencies();
        }

        #region Setup

        protected virtual ILiteCollection<T> CreateCollection()
        {
            var collection = Database.GetCollection<T>(Options.CollectionName);

            return collection;
        }
        protected virtual void SetupDependencies()
        {
            foreach (var dependency in RepositoryDependencies)
            {
                dependency.RefreshNeeded += OnRefreshNeeded;
                dependency.Apply(Collection);
            }
        }

        protected virtual void OnRefreshNeeded(NeedsRefresh<T> needsRefresh)
        {
            // Get a set of keys for all entities that need a refresh
            var keysToReload = GetAll().Where(x => needsRefresh(x))
                  .Select(x => GetKey(x)).ToHashSet();

            Task.Run(() =>
            {
                try
                {
                    // Update cache for these entities
                    RefreshCacheFor(keysToReload);
                }
                catch (Exception ex)
                {
                    // Update cache failed
                }
            });
        }
        #endregion


        #region Cache
        public void RefreshCache()
        {
            _cacheLock.AcquireReaderLock(CacheTimeout);

            List<T> entitiesToUpdate;

            try
            {
                entitiesToUpdate = Collection.FindAll().ToList();
            }
            catch
            {
                _cacheLock.ReleaseReaderLock();
                throw;
            }


            RefreshCacheFor(entitiesToUpdate);
        }
        public void RefreshCacheFor(ISet<object> keys)
        {
            _cacheLock.AcquireReaderLock(CacheTimeout);

            List<T> entitiesToUpdate;

            try
            {
                entitiesToUpdate = Collection.Query().Where(x => keys.Contains(GetKey(x))).ToList();
            }
            catch
            {
                _cacheLock.ReleaseReaderLock();
                throw;
            }


            RefreshCacheFor(entitiesToUpdate);
        }
        public void RefreshCacheFor(IEnumerable<T> entities)
        {
            List<T> entitiesToUpdate = entities.ToList();

            _cacheLock.AcquireWriterLock(CacheTimeout);

            try
            {
                foreach (T entity in entitiesToUpdate)
                {
                    AddOrUpdateCache(entity);
                }
            }
            finally
            {
                _cacheLock.ReleaseWriterLock();
            }
        }

        private void AddOrUpdateCache(T entity)
        {
            object key = GetKey(entity);

            _ = Cache.AddOrUpdate(key, key => entity, (key, oldValue) => entity);
        }
        private bool AddToCache(T entity)
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

            foreach (var constraint in UniqueConstraints)
                foreach (var existingEntity in existingEntities)
                    foreach (var entity in entitiesList)
                        if (!constraint.Allows(entity, existingEntity))
                        {
                            //constraint.PropertyName
                            //TODO: Log
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
                    //constraint.PropertyName
                    //TODO: Log
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
        public T Create(T entity)
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

        public IEnumerable<T> GetAll()
        {
            return GetFromCache();
        }

#nullable enable
        public T? FindById(object id)
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

        public bool Set(T entity)
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
        public int Set(IEnumerable<T> entities)
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

        public void Update(T entity)
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

        public bool Delete(T entity)
        {
            object key = GetKey(entity);

            return Delete(key);
        }
        public bool Delete(object id)
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
                    _ = Cache.TryRemove(id, out deletedEntity);
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
        public int DeleteMany(Func<T, bool> predicate)
        {
            int counter = Collection.DeleteMany(entity => predicate(entity));

            List<T> removedEntities = new();

            foreach (var kvp in Cache.Where(kvp => predicate(kvp.Value)))
            {
                object id = kvp.Key;

                if (Cache.TryRemove(id, out var entity))
                {
                    removedEntities.Add(entity);
                    OnEntityDeleted(entity);
                    continue;
                }
            }

            OnRepositoryChanged(RepositoryChangedAction.Remove, removedItems: removedEntities);

            return counter;
        }
        public int DeleteAll()
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
        protected virtual void OnEntityUpdated(T entity)
        {
            EntityUpdated?.Invoke(entity);
        }

        protected virtual void OnEntityAdded(T entity)
        {
            EntityAdded?.Invoke(entity);
        }

        protected virtual void OnEntityDeleted(T entity)
        {
            EntityDeleted?.Invoke(entity);
        }

        protected virtual void OnRepositoryChanged(
            RepositoryChangedAction actions,
            IReadOnlyList<T> addedItems = null,
            IReadOnlyList<T> replacedItems = null,
            IReadOnlyList<T> removedItems = null)
        {
            RepositoryChanged?.Invoke(new(actions)
            {
                AddedItems = addedItems,
                ReplacedItems = replacedItems,
                RemovedItems = removedItems
            });
        }

        #endregion
    }
}
