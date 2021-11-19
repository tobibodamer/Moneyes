using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LiteDB;

namespace Moneyes.Data
{
    public class CachedRepository<T> : IBaseRepository<T>
    {
        protected ILiteDatabase DB { get; }

        private Lazy<ILiteCollection<T>> _collectionLazy;
        protected ILiteCollection<T> Collection => _collectionLazy.Value;

        private readonly Lazy<ConcurrentDictionary<object, T>> _cache;
        protected ConcurrentDictionary<object, T> Cache => _cache.Value;

        public event Action<T> EntityAdded;
        public event Action<T> EntityUpdated;
        public event Action<T> EntityDeleted;

        protected CachedRepository(IDatabaseProvider databaseProvider)
        {
            DB = databaseProvider.Database;

            _collectionLazy = new Lazy<ILiteCollection<T>>(CreateCollection);

            _cache = new(() => CreateCache());
        }

        protected virtual ILiteCollection<T> CreateCollection()
        {
            return DB.GetCollection<T>();
        }

        private ConcurrentDictionary<object, T> CreateCache()
        {
            ConcurrentDictionary<object, T> result = new();

            UpdateCacheInternal(result);

            return result;
        }

        public void UpdateCache()
        {
            UpdateCacheInternal(Cache);
        }

        private void UpdateCacheInternal(ConcurrentDictionary<object, T> cache)
        {
            foreach (T entity in Collection.FindAll())
            {
                object id = GetIdOrHash(entity);
                _ = cache.AddOrUpdate(id, id => entity, (id, oldValue) => entity);
            }
        }

        public virtual T Create(T entity)
        {
            object id = GetIdOrHash(entity);

            if (Collection.Insert(entity))
            {
                _ = Cache.AddOrUpdate(id, id => entity, (id, oldValue) => entity);

                OnEntityAdded(entity);

                return entity;
            }

            return default;
        }

        public virtual IEnumerable<T> GetAll()
        {
            return Cache.Values.ToList();
        }

        public virtual T FindById(object id)
        {
            if (Cache.TryGetValue(id, out T enitity))
            {
                return enitity;
            }

            return default;
        }

        private static object GetIdOrHash(T entity)
        {
            return IDSelectors.Resolve(entity) ?? entity.GetHashCode();
        }

        public virtual bool Set(T entity)
        {
            object id;

            if (Collection.Upsert(entity))
            {
                // Get id after insert -> auto id set
                id = GetIdOrHash(entity);

                _ = Cache.AddOrUpdate(id, id => entity, (id, oldValue) => entity);

                OnEntityAdded(entity);

                return true;
            }

            id = GetIdOrHash(entity);

            // Key exists -> update
            Cache[id] = entity;

            // Raise event directly (no id create)
            OnEntityUpdated(entity);

            return false;
        }

        public virtual int Set(IEnumerable<T> entities)
        {
            List<T> entitiesToSet = entities.ToList();

            int counter = Collection.Upsert(entitiesToSet);

            foreach (T entity in entities)
            {
                object id = GetIdOrHash(entity);

                if (Cache.TryAdd(id, entity))
                {
                    OnEntityAdded(entity);
                    continue;
                }

                // Key exists -> update
                Cache[id] = entity;

                // Raise event directly (no id create)
                OnEntityUpdated(entity);
            }

            return counter;
        }

        public virtual bool Delete(object id)
        {
            if (Collection.Delete(new BsonValue(id)))
            {
                _ = Cache.TryRemove(id, out T entity);

                OnEntityDeleted(entity);

                return true;
            }

            return false;
        }

        public virtual bool Delete(T entity)
        {
            object id = IDSelectors.Resolve(entity);

            if (id is null)
            {
                throw new KeyNotFoundException("No ID selector registered");
            }

            return id is not null && Delete(id);
        }

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
    }
}
