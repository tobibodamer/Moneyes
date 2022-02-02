using Moneyes.Core;
using System;

namespace Moneyes.Data
{  
    public interface IRepositoryFactory<T>
    {
        ICachedRepository<T> CreateRepository(
             CachedRepositoryOptions options, bool autoId);
    }

    public interface IRepositoryFactory<T, TKey> : IRepositoryFactory<T>
        where TKey : struct
    {
        ICachedRepository<T, TKey> CreateRepository(
             CachedRepositoryOptions options, Func<T, TKey> keySelector, bool autoId);
    }
}
