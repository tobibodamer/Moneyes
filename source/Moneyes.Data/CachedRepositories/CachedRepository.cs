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
using Moneyes.Core.Filters;

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
    public partial class CachedRepository<T> : ICachedRepository<T>
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
            if (e.Action is RepositoryChangedAction.Add)
            {
                // If entity was added, dont do anything, because entity is not yet present in this repository.
                return;
            }

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

        /// <summary>
        /// Exception when a constraint is violated and <see cref="ConflictResolution.Fail"/> is chosen.
        /// </summary>
        public class ConstraintViolationException : Exception
        {
            public string PropertyName { get; }
            public object NewValue { get; }
            public object ExistingValue { get; }

            public ConstraintViolationException(
                string message, string propertyName, object newValue, object existingValue)
                : base(message)
            {
                PropertyName = propertyName;
                NewValue = newValue;
                ExistingValue = existingValue;
            }
        }

        protected virtual ConstraintViolationHandler CreateConstraintViolationHandler()
        {
            return new(this, Logger);
        }


        /// <summary>
        /// Creates a function that validates an entity with the given <paramref name="uniqueConstraints"/>.
        /// </summary>
        /// <param name="existingEntities">The existing entities to validate against.</param>
        /// <param name="uniqueConstraints">The unique constraints to validate.</param>
        /// <param name="onViolation">A handler that is called on violation that decides how to handle the violation.</param>
        /// <returns></returns>
        protected virtual Func<T, bool> CreateUniqueConstraintValidator(
            IEnumerable<T> existingEntities,
            IEnumerable<IUniqueConstraint<T>> uniqueConstraints,
            Func<ConstraintViolation<T>, (bool continueValidation, bool ignore)> onViolation)
        {
            Dictionary<IUniqueConstraint<T>, Dictionary<object, T>> constraintMap = new();

            // Go through each unique constraint
            foreach (var constraint in uniqueConstraints)
            {
                var constraintPropertyIndeces = new Dictionary<object, T>();

                // Add the constraint property values of existing entities to a map
                foreach (var existingEntity in existingEntities)
                {
                    constraintPropertyIndeces.Add(constraint.GetPropertyValue(existingEntity), existingEntity);
                }

                constraintMap[constraint] = constraintPropertyIndeces;
            }

            // Create and return the validation function
            return (entity) =>
            {
                bool isValid = true; // By default, entity is valid

                // Check all constaints
                foreach (var constraint in UniqueConstraints)
                {
                    var constraintValues = constraintMap[constraint];
                    var uniquePropertyValue = constraint.GetPropertyValue(entity);

                    // Check if unique value is already existing
                    if (constraintValues.TryGetValue(uniquePropertyValue, out var existingEntity))
                    {
                        Logger.LogWarning("Unique constraint violation of '{Property}' (Entity ID: {id})",
                                                constraint.PropertyName, GetKey(entity));

                        ConstraintViolation<T> violation = new
                        (
                            constraint,
                            existingEntity,
                            entity
                        );

                        // Call violation handler function
                        var (continueValidation, ignore) = onViolation(violation);

                        if (!continueValidation)
                        {
                            // Dont continue validation -> return valid if violation should be ignored
                            return ignore;
                        }

                        // If violation ignored -> entity is still valid
                        isValid = ignore;
                    }
                }

                // Return true if the entity is valid
                return isValid;
            };
        }



        protected virtual void ValidatePrimaryKey(object key, IEnumerable<T> existingEntities = null)
        {
            existingEntities ??= GetFromCache().ToArray();

            foreach (var existingEntity in existingEntities)
            {
                if (GetKey(existingEntity).Equals(key))
                {
                    Logger.LogWarning("Primary key violation: {key} already exists", key);

                    throw new DuplicateKeyException("Primary key already exists", key);
                }
            }
        }

        protected virtual void ValidatePrimaryKeys(IReadOnlySet<object> keys, IEnumerable<T> existingEntities = null)
        {
            if (!IsUnique(keys))
            {
                // Duplicate PK in collection itself
                throw new DuplicateKeyException("Duplicate primary key in given collection.");
            }

            existingEntities ??= GetFromCache().ToArray();

            foreach (var existingEntity in existingEntities)
            {
                var key = GetKey(existingEntity);

                if (keys.Contains(key))
                {
                    Logger.LogWarning("Primary key violation: {key} already exists", key);

                    throw new DuplicateKeyException("Primary key already exists", key);
                }
            }
        }

        private static bool IsUnique<K>(IEnumerable<K> list)
        {
            var hs = new HashSet<K>();
            return list.All(hs.Add);
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

                ValidatePrimaryKey(key);
            }

            // Create the contraint validation handler
            var constraintViolationHandler = CreateConstraintViolationHandler();

            // Create validator function
            var validateUniqueConstraints = CreateUniqueConstraintValidator(
                existingEntities: GetFromCache(),
                uniqueConstraints: UniqueConstraints,
                onViolation: v =>
                {
                    // Use the constraint violation handler to handle this violation
                    return constraintViolationHandler.Handle(v, null);
                });

            _cacheLock.AcquireWriterLock(CacheTimeout);

            T createdEntity = default;

            try
            {
                if (validateUniqueConstraints(entity))
                {
                    var bsonKey = Collection.Insert(entity);
                    createdEntity = Collection.FindById(bsonKey);

                    key = bsonKey.RawValue;

                    // Update cache with created entity
                    AddOrUpdateCache(createdEntity);
                }
            }
            finally
            {
                _cacheLock.ReleaseWriterLock();
            }

            if (createdEntity == null)
            {
                return default;
            }

            OnEntityAdded(createdEntity);
            OnRepositoryChanged(RepositoryChangedAction.Add, addedItems: new List<T>() { createdEntity });

            return createdEntity;
        }

        public virtual int Create(IEnumerable<T> entities)
        {
            return Create(entities, null);
        }
        public virtual int Create(IEnumerable<T> entities, Func<ConstraintViolation<T>, ConflictResolutionAction> onConflict)
        {
            // Get the primary keys of the entities to upsert
            var keys = entities.Select(GetKey).ToHashSet();

            if (!IsAutoId)
            {
                ValidatePrimaryKeys(keys);
            }

            // Create the contraint validation handler
            var constraintViolationHandler = CreateConstraintViolationHandler();

            // Create validator function
            var validateUniqueConstraints = CreateUniqueConstraintValidator(
                existingEntities: GetFromCache(),
                uniqueConstraints: UniqueConstraints,
                onViolation: v =>
                {
                    // Call user conflict resolution delegate if provided
                    var userConflictResolution = onConflict?.Invoke(v);

                    // Use the constraint violation handler to handle this violation
                    return constraintViolationHandler.Handle(v, userConflictResolution);
                });

            // Validate unique constraints -> get all valid entities
            var entitiesToInsert = entities.Where(e => validateUniqueConstraints(e)).ToList();


            _cacheLock.AcquireWriterLock(CacheTimeout);

            int counter = 0;
            List<T> addedEntities = new();

            try
            {
                counter = Collection.Insert(entitiesToInsert);

                foreach (T entity in entitiesToInsert)
                {
                    object id = GetKey(entity);

                    if (AddToCache(entity))
                    {
                        addedEntities.Add(entity);
                    }
                }
            }
            finally
            {
                _cacheLock.ReleaseWriterLock();
            }


            // Now perform deletes and updates of violated constraints
            constraintViolationHandler.PerformDeletes();
            constraintViolationHandler.PerformUpdates();

            // Notify

            foreach (var entity in addedEntities)
            {
                OnEntityAdded(entity);
            }

            if (addedEntities.Any())
            {
                OnRepositoryChanged(RepositoryChangedAction.Add, addedItems: addedEntities);
            }

            return counter;
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

        public virtual IReadOnlyList<T> FindAllById(params object[] ids)
        {
            ArgumentNullException.ThrowIfNull(ids);

            if (ids.Length == 0)
            {
                return new List<T>();
            }

            return ids
                .Select(id => Cache.GetValueOrDefault(id))
                .Where(x => x != null)
                .ToList();
        }

        public virtual bool Contains(object id)
        {
            ArgumentNullException.ThrowIfNull(id);

            return Cache.ContainsKey(id);
        }

        public virtual bool ContainsAll(params object[] ids)
        {
            ArgumentNullException.ThrowIfNull(ids);

            return ids.All(id => Cache.ContainsKey(id));
        }

        public virtual bool ContainsAny(params object[] ids)
        {
            ArgumentNullException.ThrowIfNull(ids);

            return ids.Any(id => Cache.ContainsKey(id));
        }

        public virtual bool Set(T entity, Func<ConstraintViolation<T>, ConflictResolutionAction>? onConflict)
        {
            ArgumentNullException.ThrowIfNull(entity);

            object key = GetKey(entity);
            bool? inserted = null;

            // Dont validate primary key, because might be update            

            // Get all entities that will not be replaced
            var otherEntities = Cache.Where(x => !key.Equals(x.Key)).Select(x => x.Value);

            // Create the contraint validation handler
            var constraintViolationHandler = CreateConstraintViolationHandler();

            // Create validator function
            var validateUniqueConstraints = CreateUniqueConstraintValidator(
                existingEntities: otherEntities,
                uniqueConstraints: UniqueConstraints,
                onViolation: v =>
                {
                    // Call user conflict resolution delegate if provided
                    var userConflictResolution = onConflict?.Invoke(v);

                    // Use the constraint violation handler to handle this violation
                    return constraintViolationHandler.Handle(v, userConflictResolution);
                });

            try
            {
                _cacheLock.AcquireWriterLock(CacheTimeout);

                if (validateUniqueConstraints(entity))
                {
                    // Upsert in db
                    inserted = Collection.Upsert(entity);

                    // Get key after upsert (might be not set until auto id)
                    key = GetKey(entity);

                    // Upsert in cache
                    AddOrUpdateCache(entity);
                }

                // Now perform deletes and updates of violated constraints
                constraintViolationHandler.PerformDeletes();
                constraintViolationHandler.PerformUpdates();
            }
            finally
            {
                _cacheLock.ReleaseWriterLock();
            }

            // Notify

            if (inserted is null)
            {
                return false;
            }

            if (inserted.Value)
            {
                OnEntityAdded(entity);
                OnRepositoryChanged(RepositoryChangedAction.Add, addedItems: new List<T>() { entity });
            }
            else
            {
                OnEntityUpdated(entity);
                OnRepositoryChanged(RepositoryChangedAction.Replace, replacedItems: new List<T>() { entity });
            }

            return inserted.Value;
        }
        public virtual bool Set(T entity)
        {
            return Set(entity, null);
        }
        public virtual int Set(IEnumerable<T> entities, Func<ConstraintViolation<T>, ConflictResolutionAction>? onConflict)
        {
            // Get the primary keys of the entities to upsert
            var keys = entities.Select(GetKey).ToHashSet();

            // Get all entities that will not be replaced
            var otherEntities = Cache.Where(x => !keys.Contains(x.Key)).Select(x => x.Value);

            // Validate primary keys, just among themselves
            ValidatePrimaryKeys(keys, otherEntities);

            // Create the contraint validation handler
            var constraintViolationHandler = CreateConstraintViolationHandler();

            // Create validator function
            var validateUniqueConstraints = CreateUniqueConstraintValidator(
                existingEntities: otherEntities,
                uniqueConstraints: UniqueConstraints,
                onViolation: v =>
                {
                    // Call user conflict resolution delegate if provided
                    var userConflictResolution = onConflict?.Invoke(v);

                    // Use the constraint violation handler to handle this violation
                    return constraintViolationHandler.Handle(v, userConflictResolution);
                });

            // Validate unique constraints -> get all valid entities
            var entitiesToUpsert = entities.Where(e => validateUniqueConstraints(e)).ToList();

            int counter = 0;
            List<T> addedEntities = new();
            List<T> updatedEntities = new();

            _cacheLock.AcquireWriterLock(CacheTimeout);
            try
            {
                counter = Collection.Upsert(entitiesToUpsert);

                foreach (T entity in entitiesToUpsert)
                {
                    object id = GetKey(entity);

                    if (AddToCache(entity))
                    {
                        addedEntities.Add(entity);
                        continue;
                    }

                    //T oldValue = Cache[id];

                    // Key exists -> update
                    AddOrUpdateCache(entity);

                    //if (!oldValue.Equals(entity))
                    //{
                    updatedEntities.Add(entity);
                    //}
                }
            }
            finally
            {
                _cacheLock.ReleaseWriterLock();
            }

            // Now perform deletes and updates of violated constraints
            constraintViolationHandler.PerformDeletes();
            constraintViolationHandler.PerformUpdates();

            // Notify

            foreach (var entity in addedEntities)
            {
                OnEntityAdded(entity);
            }

            foreach (var entity in updatedEntities)
            {
                OnEntityUpdated(entity);
            }

            if (addedEntities.Any() || updatedEntities.Any())
            {
                OnRepositoryChanged(RepositoryChangedAction.Add | RepositoryChangedAction.Replace,
                    addedItems: addedEntities,
                    replacedItems: updatedEntities);
            }

            return counter;
        }
        public virtual int Set(IEnumerable<T> entities)
        {
            return Set(entities, null);
        }

        public virtual void Update(T entity)
        {
            object key = GetKey(entity);

            ArgumentNullException.ThrowIfNull(key);

            try
            {
                _cacheLock.AcquireReaderLock(CacheTimeout);

                // Check if entity exists
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


            // Get all entities that will not be replaced
            var otherEntities = Cache.Where(x => !key.Equals(x.Key)).Select(x => x.Value);

            // Create the contraint validation handler
            var constraintViolationHandler = CreateConstraintViolationHandler();

            // Create validator function
            var validateUniqueConstraints = CreateUniqueConstraintValidator(
                existingEntities: otherEntities,
                uniqueConstraints: UniqueConstraints,
                onViolation: v =>
                {
                    // Use the constraint violation handler to handle this violation
                    return constraintViolationHandler.Handle(v, null);
                });

            bool updated = false;

            _cacheLock.UpgradeToWriterLock(CacheTimeout);
            try
            {
                if (validateUniqueConstraints(entity))
                {
                    // Upsert in db
                    updated = Collection.Update(entity);

                    // Get key after update (might be not set until auto id)
                    key = GetKey(entity);

                    // Upsert in cache
                    AddOrUpdateCache(entity);
                }
            }
            finally
            {
                _cacheLock.ReleaseWriterLock();
            }

            // Now perform deletes and updates of violated constraints
            constraintViolationHandler.PerformDeletes();
            constraintViolationHandler.PerformUpdates();

            if (updated)
            {
                OnEntityUpdated(entity);
                OnRepositoryChanged(RepositoryChangedAction.Replace, replacedItems: new List<T>() { entity });
            }
        }

        public virtual bool Update(object id, Func<T, T> updateFactory)
        {
            ArgumentNullException.ThrowIfNull(id);
            ArgumentNullException.ThrowIfNull(updateFactory);

            try
            {
                _cacheLock.AcquireReaderLock(CacheTimeout);

                // Check if entity exists
                if (!Cache.ContainsKey(id))
                {
                    throw new KeyNotFoundException();
                }
            }
            catch
            {
                _cacheLock.ReleaseReaderLock();
                throw;
            }

            if (!Cache.TryGetValue(id, out var existingEntity))
            {
                return false;
            }

            T entity = updateFactory(existingEntity);

            // Get all entities that will not be replaced
            var otherEntities = Cache.Where(x => !id.Equals(x.Key)).Select(x => x.Value);

            // Create the contraint validation handler
            var constraintViolationHandler = CreateConstraintViolationHandler();

            // Create validator function
            var validateUniqueConstraints = CreateUniqueConstraintValidator(
                existingEntities: otherEntities,
                uniqueConstraints: UniqueConstraints,
                onViolation: v =>
                {
                    // Use the constraint violation handler to handle this violation
                    return constraintViolationHandler.Handle(v, null);
                });

            bool updated = false;

            _cacheLock.UpgradeToWriterLock(CacheTimeout);
            try
            {
                if (validateUniqueConstraints(entity))
                {
                    // Upsert in db
                    updated = Collection.Update(entity);

                    // Get key after update (might be not set until auto id)
                    id = GetKey(entity);

                    // Upsert in cache
                    AddOrUpdateCache(entity);
                }
            }
            finally
            {
                _cacheLock.ReleaseWriterLock();
            }

            // Now perform deletes and updates of violated constraints
            constraintViolationHandler.PerformDeletes();
            constraintViolationHandler.PerformUpdates();

            if (updated)
            {
                OnEntityUpdated(entity);
                OnRepositoryChanged(RepositoryChangedAction.Replace, replacedItems: new List<T>() { entity });
            }

            return updated;
        }
        public virtual int Update(IEnumerable<T> entities)
        {
            var keys = entities.Select(GetKey).ToHashSet();

            // Check if all entities exist
            if (keys.Any(key => !Cache.ContainsKey(key)))
            {
                throw new KeyNotFoundException();
            }

            // Get all entities that will not be replaced
            var otherEntities = Cache.Where(x => !keys.Contains(x.Key)).Select(x => x.Value);

            // Create the contraint validation handler
            var constraintViolationHandler = CreateConstraintViolationHandler();

            // Create validator function
            var validateUniqueConstraints = CreateUniqueConstraintValidator(
                existingEntities: otherEntities,
                uniqueConstraints: UniqueConstraints,
                onViolation: v =>
                {
                    // Use the constraint violation handler to handle this violation
                    return constraintViolationHandler.Handle(v, null);
                });

            // Validate unique constraints -> get all valid entities
            var entitiesToUpdate = entities.Where(e => validateUniqueConstraints(e)).ToList();

            int counter = 0;

            _cacheLock.AcquireWriterLock(CacheTimeout);
            try
            {
                // Update in database
                counter = Collection.Update(entitiesToUpdate);

                // Update in cache
                foreach (T entity in entitiesToUpdate)
                {
                    AddOrUpdateCache(entity);
                }
            }
            finally
            {
                _cacheLock.ReleaseWriterLock();
            }

            // Now perform deletes and updates of violated constraints
            constraintViolationHandler.PerformDeletes();
            constraintViolationHandler.PerformUpdates();

            // Notify
            foreach (T entity in entitiesToUpdate)
            {
                OnEntityUpdated(entity);
            }

            if (entitiesToUpdate.Any())
            {
                OnRepositoryChanged(RepositoryChangedAction.Replace,
                    replacedItems: entitiesToUpdate);
            }

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

            var compiledPredicate = predicate.Compile();

            foreach (var kvp in Cache.Where(kvp => compiledPredicate(kvp.Value)).ToList())
            {
                object id = kvp.Key;

                if (Cache.Remove(id, out var entity))
                {
                    removedEntities.Add(entity);

                    Logger.LogInformation("Deleted entity with id '{key}'.", id);

                    OnEntityDeleted(entity);
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
