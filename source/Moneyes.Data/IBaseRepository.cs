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

        event Action<T> EntityAdded;
        event Action<T> EntityUpdated;
        event Action<T> EntityDeleted;
    }
}