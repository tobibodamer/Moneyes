using System;
using System.Collections.Generic;

namespace Moneyes.Data
{
    public interface ICachedRepository<T> : IBaseRepository<T>
    {
        string CollectionName { get; }
        object GetKey(T entity);
        void Update(T entity);

        void RefreshCache();

        void RefreshCacheFor(IEnumerable<T> entities);

        event Action<RepositoryChangedEventArgs<T>> RepositoryChanged;
    }

    public interface ICachedRepository<T, TKey> : ICachedRepository<T> where TKey : struct
    {
#nullable enable
        T? FindById(TKey id);
#nullable disable

        bool Delete(T entity);
        bool DeleteById(TKey id);

        new TKey GetKey(T entity);
    }
}
