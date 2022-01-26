namespace Moneyes.Data
{
    public interface IRepositoryProvider
    {
        ICachedRepository<T> GetRepository<T>(string collectionName = null);

        ICachedRepository<T, TKey> GetRepository<T, TKey>(string collectionName = null) where TKey : struct;
    }
}
