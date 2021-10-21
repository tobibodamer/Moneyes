using System.Collections.Generic;

namespace Moneyes.Data
{
    public interface IBaseRepository<T>
    {
        T Create(T data);
        IEnumerable<T> All();
        T FindById(int id);
        void Set(T entity);
        bool Delete(int id);
    }
}
