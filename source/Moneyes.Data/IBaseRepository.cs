using System.Collections.Generic;

namespace Moneyes.Data
{
    public interface IBaseRepository<T>
    {
        T Create(T data);
        IEnumerable<T> All();
        T FindById(int id);
        T FindById(string id);
        bool Set(T entity);
        void Set(IEnumerable<T> entities);
        bool Delete(int id);
    }
}
