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
    public partial class CachedRepository<T, TKey> : ICachedRepository<T, TKey>
        where TKey : struct
    {
        private readonly Lazy<ILiteCollection<T>> _collectionLazy;
        private readonly Func<T, TKey> _keySelector;
        private readonly ReaderWriterLock _cacheLock = new();
        public bool IsAutoId { get; }
        public string CollectionName => Options.CollectionName;
#nullable enable
        public Func<RepositoryOperation, ConflictResolutionDelegate<T>>? DefaultConflictHandler { get; set; }
#nullable disable
        protected ILiteDatabase Database { get; }
        protected ILiteCollection<T> Collection => _collectionLazy.Value;
        protected IEnumerable<IRepositoryDependency<T>> RepositoryDependencies { get; set; }
        protected IEnumerable<IUniqueConstraint<T>> UniqueConstraints { get; set; }
        protected CachedRepositoryOptions Options { get; }
        protected DependencyRefreshHandler DependencyRefreshHandler { get; }
        protected Dictionary<TKey, T> Cache { get; } = new();

        protected ILogger<CachedRepository<T, TKey>> Logger { get; }

        public event Action<T> EntityAdded;
        public event Action<T> EntityUpdated;
        public event Action<T> EntityDeleted;
        public event Action<RepositoryChangedEventArgs<T>> RepositoryChanged;

        private const int CacheTimeout = 2000;
        public CachedRepository(
            IDatabaseProvider<ILiteDatabase> databaseProvider,
            CachedRepositoryOptions options,
            DependencyRefreshHandler refreshHandler,
            Func<T, TKey> keySelector,
            IEnumerable<IRepositoryDependency<T>> repositoryDependencies = null,
            IEnumerable<IUniqueConstraint<T>> uniqueConstraints = null,
            ILogger<CachedRepository<T, TKey>> logger = null)
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

        public virtual TKey GetKey(T entity)
        {
            return _keySelector(entity);
        }

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
            TKey key = GetKey(entity);

            Cache[key] = entity;
        }
        protected bool AddToCache(T entity)
        {
            TKey key = GetKey(entity);

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
            var constraintValues = uniqueConstraints
                .Select(constraint => (constraint, values: new Dictionary<object, T>()))
                .ToList();

            // Add the constraint property values of existing entities to a map
            foreach (var existingEntity in existingEntities)
            {
                // Go through each unique constraint
                foreach (var (constraint, values) in constraintValues)
                {
                    // Add unique property value
                    values.Add(constraint.GetPropertyValue(existingEntity), existingEntity);
                }
            }

            // Create and return the validation function
            return (entity) =>
            {
                bool isValid = true; // By default, entity is valid

                // Check all constaints
                foreach (var (constraint, values) in constraintValues)
                {
                    var uniquePropertyValue = constraint.GetPropertyValue(entity);

                    // Check if unique value is already existing
                    if (values.TryGetValue(uniquePropertyValue, out var existingEntity))
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

        protected virtual void ValidatePrimaryKey(TKey key, IEnumerable<T> existingEntities = null)
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

        protected virtual void ValidatePrimaryKeys(IReadOnlySet<TKey> keys, IEnumerable<T> existingEntities = null)
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



        #region CRUD
        public virtual T Create(T entity)
        {
            TKey key;

            if (!IsAutoId)
            {
                key = GetKey(entity);

                ValidatePrimaryKey(key);
            }

            // Create the contraint validation handler
            var constraintViolationHandler = CreateConstraintViolationHandler();

            // Create validator function
            var validateUniqueConstraints = CreateDefaultUniqueConstraintValidator(
                repositoryOperation: RepositoryOperation.Create,
                existingEntities: GetFromCache(),
                constraintViolationHandler);

            _cacheLock.AcquireWriterLock(CacheTimeout);

            T createdEntity = default;

            try
            {
                if (validateUniqueConstraints(entity))
                {
                    var bsonKey = Collection.Insert(entity);
                    createdEntity = Collection.FindById(bsonKey);

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

        public virtual int CreateMany(IEnumerable<T> entities)
        {
            return CreateMany(entities, null);
        }
        public virtual int CreateMany(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict)
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
            var validateUniqueConstraints = CreateDefaultUniqueConstraintValidator(
                repositoryOperation: RepositoryOperation.Create,
                existingEntities: GetFromCache(),
                constraintViolationHandler,
                onConflict);

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
                    TKey id = GetKey(entity);

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
        public virtual T? FindById(TKey id)
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

        public virtual IReadOnlyList<T> FindAllById(params TKey[] ids)
        {
            ArgumentNullException.ThrowIfNull(ids);

            if (ids.Length == 0)
            {
                return new List<T>();
            }

            List<T> result = new();

            foreach (var id in ids)
            {
                if (Cache.TryGetValue(id, out var entity))
                {
                    result.Add(entity);
                }
            }

            return result.AsReadOnly();
        }

        public virtual bool Contains(TKey id)
        {
            ArgumentNullException.ThrowIfNull(id);

            return Cache.ContainsKey(id);
        }

        public virtual bool ContainsAll(params TKey[] ids)
        {
            ArgumentNullException.ThrowIfNull(ids);

            return ids.All(id => Cache.ContainsKey(id));
        }

        public virtual bool ContainsAny(params TKey[] ids)
        {
            ArgumentNullException.ThrowIfNull(ids);

            return ids.Any(id => Cache.ContainsKey(id));
        }


        public virtual bool Set(T entity)
        {
            return Set(entity, null);
        }
        public virtual bool Set(T entity, ConflictResolutionDelegate<T> onConflict)
        {
            ArgumentNullException.ThrowIfNull(entity);

            TKey key = GetKey(entity);

            // Dont validate primary key, because might be update            

            // Get all entities that will not be replaced
            var otherEntities = Cache.Where(x => !key.Equals(x.Key)).Select(x => x.Value);

            // Create the contraint validation handler
            var constraintViolationHandler = CreateConstraintViolationHandler();

            // Create validator function
            var validateUniqueConstraints = CreateDefaultUniqueConstraintValidator(
                repositoryOperation: RepositoryOperation.Upsert,
                existingEntities: otherEntities,
                constraintViolationHandler,
                onConflict);

            if (!validateUniqueConstraints(entity))
            {
                return false;
            }

            return SetInternal(entity, constraintViolationHandler);
        }
        public virtual bool Set(TKey id,
            Func<TKey, T> addEntityFactory,
            Func<TKey, T, T> updateEntityFactory,
            ConflictResolutionDelegate<T> onConflict = null)
        {
            // Create entity using add and update entity factories
            var entity = CreateEntityFromFactory(id, addEntityFactory, updateEntityFactory);

            return Set(entity, onConflict);
        }

        protected virtual bool Set(T entity,
           Func<T, T> addEntityFactory,
           Func<T, T, T> updateEntityFactory,
           ConflictResolutionDelegate<T> onConflict = null)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(addEntityFactory);
            ArgumentNullException.ThrowIfNull(updateEntityFactory);

            TKey key = GetKey(entity);

            entity = CreateEntityFromFactory(KeyValuePair.Create(key, entity), addEntityFactory, updateEntityFactory);

            // Dont validate primary key, because might be update            

            // Get all entities that will not be replaced
            var otherEntities = Cache.Where(x => !key.Equals(x.Key)).Select(x => x.Value);

            // Create the contraint validation handler
            var constraintViolationHandler = CreateConstraintViolationHandler();

            // Create validator function
            var validateUniqueConstraints = CreateDefaultUniqueConstraintValidator(
                repositoryOperation: RepositoryOperation.Upsert,
                existingEntities: otherEntities,
                constraintViolationHandler,
                onConflict);

            if (!validateUniqueConstraints(entity))
            {
                return false;
            }

            return SetInternal(entity, constraintViolationHandler);
        }

        private bool SetInternal(T validatedEntity, ConstraintViolationHandler constraintViolationHandler)
        {
            bool inserted;

            _cacheLock.AcquireWriterLock(CacheTimeout);
            try
            {
                // Upsert in db
                inserted = Collection.Upsert(validatedEntity);

                // Upsert in cache
                AddOrUpdateCache(validatedEntity);
            }
            finally
            {
                _cacheLock.ReleaseWriterLock();
            }

            // Now perform deletes and updates of violated constraints
            constraintViolationHandler.PerformDeletes();
            constraintViolationHandler.PerformUpdates();

            // Notify

            if (inserted)
            {
                OnEntityAdded(validatedEntity);
                OnRepositoryChanged(RepositoryChangedAction.Add, addedItems: new List<T>() { validatedEntity });
            }
            else
            {
                OnEntityUpdated(validatedEntity);
                OnRepositoryChanged(RepositoryChangedAction.Replace, replacedItems: new List<T>() { validatedEntity });
            }

            return inserted;
        }

        public virtual int SetMany(IEnumerable<T> entities)
        {
            return SetMany(entities, null);
        }
        public virtual int SetMany(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict)
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
            var validateUniqueConstraints = CreateDefaultUniqueConstraintValidator(
                repositoryOperation: RepositoryOperation.Upsert,
                existingEntities: otherEntities,
                constraintViolationHandler,
                onConflict);

            // Validate unique constraints -> get all valid entities
            var entitiesToUpsert = entities.Where(e => validateUniqueConstraints(e)).ToList();

            return SetInternal(entitiesToUpsert, constraintViolationHandler);
        }

        public virtual int SetMany(IEnumerable<TKey> ids,
            Func<TKey, T> addEntityFactory,
            Func<TKey, T, T> updateEntityFactory,
            ConflictResolutionDelegate<T> onConflict = null)
        {
            var keys = ids.ToHashSet();

            // Create entities using add and update entity factories
            var entities = keys.Select(key => CreateEntityFromFactory(key, addEntityFactory, updateEntityFactory));

            // Get all entities that will not be replaced
            var otherEntities = Cache.Where(x => !keys.Contains(x.Key)).Select(x => x.Value);

            // Validate primary keys, just among themselves
            ValidatePrimaryKeys(keys, otherEntities);

            // Create the contraint validation handler
            var constraintViolationHandler = CreateConstraintViolationHandler();

            // Create validator function
            var validateUniqueConstraints = CreateDefaultUniqueConstraintValidator(
                repositoryOperation: RepositoryOperation.Upsert,
                existingEntities: otherEntities,
                constraintViolationHandler,
                onConflict);

            // Validate unique constraints -> get all valid entities
            var entitiesToUpsert = entities.Where(e => validateUniqueConstraints(e)).ToList();

            return SetInternal(entitiesToUpsert, constraintViolationHandler);
        }

        protected virtual int SetMany(IEnumerable<T> entities,
           Func<T, T> addEntityFactory,
           Func<T, T, T> updateEntityFactory,
           ConflictResolutionDelegate<T> onConflict = null)
        {
            ArgumentNullException.ThrowIfNull(entities);
            ArgumentNullException.ThrowIfNull(addEntityFactory);
            ArgumentNullException.ThrowIfNull(updateEntityFactory);

            var keyEntityMap = entities.ToDictionary(x => GetKey(x), x => x);

            // Create entities using add and update entity factories
            entities = keyEntityMap.Select(kvp => CreateEntityFromFactory(kvp, addEntityFactory, updateEntityFactory));

            // Get all entities that will not be replaced
            var otherEntities = Cache.Where(x => !keyEntityMap.ContainsKey(x.Key)).Select(x => x.Value);

            // Validate primary keys, just among themselves
            ValidatePrimaryKeys(keyEntityMap.Keys.ToHashSet(), otherEntities);

            // Create the contraint validation handler
            var constraintViolationHandler = CreateConstraintViolationHandler();

            // Create validator function
            var validateUniqueConstraints = CreateDefaultUniqueConstraintValidator(
                repositoryOperation: RepositoryOperation.Upsert,
                existingEntities: otherEntities,
                constraintViolationHandler,
                onConflict);

            // Validate unique constraints -> get all valid entities
            var entitiesToUpsert = entities.Where(e => validateUniqueConstraints(e)).ToList();

            return SetInternal(entitiesToUpsert, constraintViolationHandler);
        }
        private int SetInternal(IEnumerable<T> entities, ConstraintViolationHandler constraintViolationHandler)
        {
            int counter = 0;
            List<T> addedEntities = new();
            List<T> updatedEntities = new();

            _cacheLock.AcquireWriterLock(CacheTimeout);
            try
            {
                counter = Collection.Upsert(entities);

                foreach (T entity in entities)
                {
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

        public virtual bool Update(T entity)
        {
            return Update(entity, onConflict: null);
        }
        public virtual bool Update(T entity, ConflictResolutionDelegate<T> onConflict)
        {
            ArgumentNullException.ThrowIfNull(entity);

            TKey key = GetKey(entity);

            _cacheLock.AcquireReaderLock(CacheTimeout);
            try
            {
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
            var validateUniqueConstraints = CreateDefaultUniqueConstraintValidator(
                repositoryOperation: RepositoryOperation.Update,
                existingEntities: otherEntities,
                constraintViolationHandler,
                onConflict);

            if (!validateUniqueConstraints(entity))
            {
                return false;
            }

            return UpdateInternal(entity, constraintViolationHandler);
        }
        public virtual bool Update(TKey id, Func<TKey, T, T> updateEntityFactory,
            ConflictResolutionDelegate<T> onConflict = null)
        {
            ArgumentNullException.ThrowIfNull(id);
            ArgumentNullException.ThrowIfNull(updateEntityFactory);

            T entity = CheckKeyAndCreateEntity(id, updateEntityFactory);

            // Get all entities that will not be replaced
            var otherEntities = Cache.Where(x => !id.Equals(x.Key)).Select(x => x.Value);

            // Create the contraint validation handler
            var constraintViolationHandler = CreateConstraintViolationHandler();

            // Create validator function
            var validateUniqueConstraints = CreateDefaultUniqueConstraintValidator(
                repositoryOperation: RepositoryOperation.Update,
                existingEntities: otherEntities,
                constraintViolationHandler,
                onConflict);

            if (!validateUniqueConstraints(entity))
            {
                return false;
            }

            return UpdateInternal(entity, constraintViolationHandler);
        }
        public virtual bool Update(T entity, Func<T, T, T> updateEntityFactory,
            ConflictResolutionDelegate<T> onConflict = null)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(updateEntityFactory);

            var key = GetKey(entity);

            entity = CheckKeyAndCreateEntity(KeyValuePair.Create(key, entity), updateEntityFactory);

            // Get all entities that will not be replaced
            var otherEntities = Cache.Where(x => !key.Equals(x.Key)).Select(x => x.Value);

            // Create the contraint validation handler
            var constraintViolationHandler = CreateConstraintViolationHandler();

            // Create validator function
            var validateUniqueConstraints = CreateDefaultUniqueConstraintValidator(
                repositoryOperation: RepositoryOperation.Update,
                existingEntities: otherEntities,
                constraintViolationHandler,
                onConflict);

            if (!validateUniqueConstraints(entity))
            {
                return false;
            }

            return UpdateInternal(entity, constraintViolationHandler);
        }
        private bool UpdateInternal(T validatedEntity, ConstraintViolationHandler constraintViolationHandler)
        {
            _cacheLock.UpgradeToWriterLock(CacheTimeout);
            try
            {
                // Upsert in db
                if (!Collection.Update(validatedEntity))
                {
                    return false;
                }

                // Upsert in cache
                AddOrUpdateCache(validatedEntity);
            }
            finally
            {
                _cacheLock.ReleaseWriterLock();
            }

            // Now perform deletes and updates of violated constraints
            constraintViolationHandler.PerformDeletes();
            constraintViolationHandler.PerformUpdates();

            OnEntityUpdated(validatedEntity);
            OnRepositoryChanged(RepositoryChangedAction.Replace, replacedItems: new List<T>() { validatedEntity });

            return true;
        }

        public virtual int UpdateMany(IEnumerable<T> entities)
        {
            return UpdateMany(entities, onConflict: null);
        }
        public virtual int UpdateMany(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict)
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
            var validateUniqueConstraints = CreateDefaultUniqueConstraintValidator(
                repositoryOperation: RepositoryOperation.Update,
                existingEntities: otherEntities,
                constraintViolationHandler,
                onConflict);

            // Validate unique constraints -> get all valid entities
            var entitiesToUpdate = entities.Where(e => validateUniqueConstraints(e)).ToList();

            return UpdateInternal(entitiesToUpdate, constraintViolationHandler);
        }
        public virtual int UpdateMany(IEnumerable<TKey> ids, Func<TKey, T, T> updateEntityFactory,
            ConflictResolutionDelegate<T> onConflict = null)
        {
            ArgumentNullException.ThrowIfNull(ids);
            ArgumentNullException.ThrowIfNull(updateEntityFactory);

            var keys = ids.ToHashSet();

            // Check keys exist and create entities
            var entities = keys.Select(key => CheckKeyAndCreateEntity(key, updateEntityFactory));

            // Get all entities that will not be replaced
            var otherEntities = Cache.Where(x => !keys.Contains(x.Key)).Select(x => x.Value);

            // Create the contraint validation handler
            var constraintViolationHandler = CreateConstraintViolationHandler();

            // Create validator function
            var validateUniqueConstraints = CreateDefaultUniqueConstraintValidator(
                repositoryOperation: RepositoryOperation.Update,
                existingEntities: otherEntities,
                constraintViolationHandler,
                onConflict);

            // Validate unique constraints -> get all valid entities
            var entitiesToUpdate = entities.Where(e => validateUniqueConstraints(e)).ToList();

            return UpdateInternal(entitiesToUpdate, constraintViolationHandler);
        }
        public virtual int UpdateMany(IEnumerable<T> entities, Func<T, T, T> updateEntityFactory,
            ConflictResolutionDelegate<T> onConflict = null)
        {
            ArgumentNullException.ThrowIfNull(entities);
            ArgumentNullException.ThrowIfNull(updateEntityFactory);

            var keyEntityMap = entities.ToDictionary(x => GetKey(x), x => x);

            // Create entities using add and update entity factories
            entities = keyEntityMap.Select(kvp => CheckKeyAndCreateEntity(kvp, updateEntityFactory));

            // Get all entities that will not be replaced
            var otherEntities = Cache.Where(x => !keyEntityMap.ContainsKey(x.Key)).Select(x => x.Value);

            // Create the contraint validation handler
            var constraintViolationHandler = CreateConstraintViolationHandler();

            // Create validator function
            var validateUniqueConstraints = CreateDefaultUniqueConstraintValidator(
                repositoryOperation: RepositoryOperation.Update,
                existingEntities: otherEntities,
                constraintViolationHandler,
                onConflict);

            // Validate unique constraints -> get all valid entities
            var entitiesToUpdate = entities.Where(e => validateUniqueConstraints(e)).ToList();

            return UpdateInternal(entitiesToUpdate, constraintViolationHandler);
        }
        private int UpdateInternal(IReadOnlyList<T> validatedEntities, ConstraintViolationHandler constraintViolationHandler)
        {
            int counter = 0;

            _cacheLock.AcquireWriterLock(CacheTimeout);
            try
            {
                // Update in database
                counter = Collection.Update(validatedEntities);

                // Update in cache
                foreach (T entity in validatedEntities)
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
            foreach (T entity in validatedEntities)
            {
                OnEntityUpdated(entity);
            }

            if (validatedEntities.Any())
            {
                OnRepositoryChanged(RepositoryChangedAction.Replace,
                    replacedItems: validatedEntities);
            }

            return counter;
        }

        /// <summary>
        /// Creates the default unique constraint validator for specific <paramref name="repositoryOperation"/>.
        /// </summary>
        /// <param name="repositoryOperation"></param>
        /// <param name="existingEntities"></param>
        /// <param name="constraintViolationHandler"></param>
        /// <param name="onConflict"></param>
        /// <returns></returns>
        private Func<T, bool> CreateDefaultUniqueConstraintValidator(
            RepositoryOperation repositoryOperation,
            IEnumerable<T> existingEntities,
            ConstraintViolationHandler constraintViolationHandler,
            ConflictResolutionDelegate<T> onConflict = null)
        {
            return CreateUniqueConstraintValidator(
                existingEntities: existingEntities,
                uniqueConstraints: UniqueConstraints,
                onViolation: v =>
                {
                    // Call user conflict resolution delegate if provided
                    var userConflictResolution = (onConflict ??
                        DefaultConflictHandler?.Invoke(repositoryOperation))?.Invoke(v);

                    // Use the constraint violation handler to handle this violation
                    return constraintViolationHandler.Handle(v, userConflictResolution);
                });
        }



        /// <summary>
        /// Function that checks if the given key exists, and creates a value using the entity factory.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="updateEntityFactory"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        private T CheckKeyAndCreateEntity(TKey key, Func<TKey, T, T> updateEntityFactory)
        {
            return CreateEntityFromFactory(key, addEntityFactory: key => throw new KeyNotFoundException(), updateEntityFactory);
        }

        /// <summary>
        /// Function that checks if the given key exists, and creates a value using the entity factory.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="updateEntityFactory"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        private T CheckKeyAndCreateEntity(KeyValuePair<TKey, T> kvp, Func<T, T, T> updateEntityFactory)
        {
            return CreateEntityFromFactory(kvp, addEntityFactory: e => throw new KeyNotFoundException(), updateEntityFactory);
        }

        /// <summary>
        /// Function that checks if the given key exists, and uses the appropriate factory to create an entity.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="addEntityFactory"></param>
        /// <param name="updateEntityFactory"></param>
        /// <returns></returns>
        private T CreateEntityFromFactory(TKey key, Func<TKey, T> addEntityFactory, Func<TKey, T, T> updateEntityFactory)
        {
            if (!Cache.TryGetValue(key, out T existing))
            {
                return addEntityFactory(key);
            }

            return updateEntityFactory(key, existing);
        }

        private T CreateEntityFromFactory(KeyValuePair<TKey, T> kvp, Func<T, T> addEntityFactory, Func<T, T, T> updateEntityFactory)
        {
            if (Cache.TryGetValue(kvp.Key, out var existing))
            {
                return updateEntityFactory(existing, kvp.Value);
            }

            return addEntityFactory(kvp.Value);
        }

        public virtual bool Delete(T entity)
        {
            TKey key = GetKey(entity);

            return DeleteById(key);
        }
        public virtual bool DeleteById(TKey id)
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
                TKey id = kvp.Key;

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

        #region Explicit implementations

        object ICachedRepository<T>.GetKey(T entity)
        {
            return GetKey(entity);
        }

        IReadOnlyList<T> ICachedRepository<T>.FindAllById(params object[] ids)
        {
            return FindAllById(ids.Cast<TKey>().ToArray());
        }

        bool ICachedRepository<T>.ContainsAny(params object[] ids)
        {
            return ContainsAny(ids.Cast<TKey>().ToArray());
        }

        bool ICachedRepository<T>.ContainsAll(params object[] ids)
        {
            return ContainsAll(ids.Cast<TKey>().ToArray());
        }

        bool ICachedRepository<T>.Contains(object id)
        {
            return Contains((TKey)id);
        }

        bool ICachedRepository<T>.Set(object id, Func<object, T> addEntityFactory, Func<object, T, T> updateEntityFactory, ConflictResolutionDelegate<T> onConflict)
        {
            return Set((TKey)id, x => addEntityFactory(x), (x, e) => updateEntityFactory(x, e), onConflict);
        }

        int ICachedRepository<T>.SetMany(IEnumerable<object> ids, Func<object, T> addEntityFactory, Func<object, T, T> updateEntityFactory, ConflictResolutionDelegate<T> onConflict)
        {
            return SetMany(ids.Cast<TKey>(), x => addEntityFactory(x), (x, e) => updateEntityFactory(x, e), onConflict);
        }

        bool ICachedRepository<T>.Update(object id, Func<object, T, T> updateEntityFactory, ConflictResolutionDelegate<T> onConflict)
        {
            return Update((TKey)id, (x, e) => updateEntityFactory(x, e), onConflict);
        }

        int ICachedRepository<T>.UpdateMany(IEnumerable<object> ids, Func<object, T, T> updateEntityFactory, ConflictResolutionDelegate<T> onConflict)
        {
            return UpdateMany(ids.Cast<TKey>(), (x, e) => updateEntityFactory(x, e), onConflict);
        }

        #endregion
    }

    public delegate ConflictResolutionAction ConflictResolutionDelegate<T>(ConstraintViolation<T> violation);
}
