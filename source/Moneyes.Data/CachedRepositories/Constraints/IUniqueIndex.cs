namespace Moneyes.Data
{
    public interface IUniqueIndex<T, TKey>
    {
        /// <summary>
        /// Gets the constraint that this index is based on.
        /// </summary>
        IUniqueConstraint<T> Constraint { get; }

        /// <summary>
        /// Creates a new copy from this index.
        /// </summary>
        IUniqueIndex<T, TKey> Copy();
        bool GetOrAddEntity(TKey key, T entity, out TKey existingKey);
        void RemoveEntity(TKey key);
        bool TryGetExistingEntity(T entity, out TKey existingKey);
        void UpdateEntity(TKey key, T entity);
    }
}