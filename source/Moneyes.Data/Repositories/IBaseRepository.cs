using System;
using System.Collections.Generic;

namespace Moneyes.Data
{
    public interface IBaseRepository<T>
    {
        T Create(T entity);
        IEnumerable<T> GetAll();
        T FindById(object id);
        bool Set(T entity);
        int Set(IEnumerable<T> entities);
        bool Delete(object id);
        int DeleteAll();
        int DeleteMany(Func<T, bool> predicate);

        event Action<T> EntityAdded;
        event Action<T> EntityUpdated;
        event Action<T> EntityDeleted;
    }
}