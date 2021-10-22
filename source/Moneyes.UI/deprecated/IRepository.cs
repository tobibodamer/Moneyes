using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moneyes.UI
{
    public interface IRepository<T> : IReadOnlyRepository<T>
    {
        Task SetAll(IEnumerable<T> items, bool overrideAlways = true);
        Task SetItem(T item, bool overrideAlways = true);
    }

    public interface IReadOnlyRepository<T>
    {
        event EventHandler<DatabaseUpdatedEventArgs<T>> Updated;

        Task<T> GetItem(object key);
        Task<IEnumerable<T>> GetAll();
    }
}