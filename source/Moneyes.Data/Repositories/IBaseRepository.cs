using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Moneyes.Data
{
    public interface IBaseRepository<T>
    {        
        T Create(T entity);
        IEnumerable<T> GetAll();
        bool Update(T entity);
        int UpdateMany(IEnumerable<T> entities);
        bool Set(T entity);
        int SetMany(IEnumerable<T> entities);
        int DeleteAll();
        int DeleteMany(Expression<Func<T, bool>> predicate);

        event Action<T> EntityAdded;
        event Action<T> EntityUpdated;
        event Action<T> EntityDeleted;
    }

    public interface IBaseRepository<T, TKey> : IBaseRepository<T>
    {
        T FindById(TKey id);
        bool DeleteById(TKey id);
    }
}