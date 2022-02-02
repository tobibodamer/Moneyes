using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Moneyes.Data
{
    public interface IBaseRepository<T>
    {        
        T Create(T entity);
        IEnumerable<T> GetAll();
        T FindById(object id);
        void Update(T entity);
        int Update(IEnumerable<T> entities);
        bool Set(T entity);
        int Set(IEnumerable<T> entities);
        bool DeleteById(object id);
        int DeleteAll();
        int DeleteMany(Expression<Func<T, bool>> predicate);

        event Action<T> EntityAdded;
        event Action<T> EntityUpdated;
        event Action<T> EntityDeleted;
    }
}