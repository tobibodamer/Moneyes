using System.Collections.Generic;
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

        public virtual T Create(T entity)
        {
            var newId = Collection.Insert(entity);
            return Collection.FindById(newId.AsInt32);
        }

        public virtual IEnumerable<T> All()
        {
            return Collection.FindAll();
        }

        public virtual T FindById(int id)
        {
            return Collection.FindById(id);
        }

        public virtual T FindById(string id)
        {
            return Collection.FindById(id);
        }

        public virtual bool Set(T entity)
        {
            return Collection.Upsert(entity);
        }

        public virtual void Set(IEnumerable<T> entities)
        {
            Collection.Upsert(entities);
        }

        public virtual bool Delete(int id)
        {
            return Collection.Delete(id);
        }
    }
}
