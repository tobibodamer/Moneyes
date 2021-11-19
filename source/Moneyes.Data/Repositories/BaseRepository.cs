using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiteDB;

namespace Moneyes.Data
{
    public abstract class BaseRepository<T> : IBaseRepository<T>
    {
        protected ILiteDatabase DB { get; }
        protected ILiteCollection<T> Collection { get; set; }

        protected BaseRepository(ILiteDatabase db)
        {
            DB = db;
            Collection = db.GetCollection<T>();
        }

        public event Action<T> EntityAdded;
        public event Action<T> EntityUpdated;
        public event Action<T> EntityDeleted;

        public virtual T Create(T entity)
        {
            BsonValue newId = Collection.Insert(entity);

            T createdEntity = Collection.FindById(newId.AsInt32);

            if (createdEntity is null)
            {
                return default;
            }

            OnEntityAdded(createdEntity);

            return createdEntity;
        }

        public virtual IEnumerable<T> GetAll()
        {
            return Collection.FindAll();
        }

        public virtual T FindById(object id)
        {
            return Collection.FindById(new BsonValue(id));
        }

        public virtual bool Set(T entity)
        {
            if (Collection.Upsert(entity))
            {
                OnEntityAdded(entity);
                return true;
            }

            OnEntityUpdated(entity);
            return false;
        }

        public virtual int Set(IEnumerable<T> entities)
        {
            int count = 0;

            foreach (T entity in entities)
            {
                if (Set(entity))
                {
                    count++;
                }
            }

            return count;
        }

        public virtual bool Delete(object id)
        {
            if (Collection.Delete(new BsonValue(id)))
            {
                OnEntityDeleted(FindById(id));
                return true;
            }

            return false;
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
