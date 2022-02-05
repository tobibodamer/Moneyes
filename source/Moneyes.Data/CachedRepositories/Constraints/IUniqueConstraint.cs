namespace Moneyes.Data
{
    public interface IUniqueConstraint<T>
    {
        /// <summary>
        /// Gets the name of the target collection this constraint applies to.
        /// </summary>
        string CollectionName { get; }

        /// <summary>
        /// Gets the name of the property this constraint applies to.
        /// </summary>
        string PropertyName { get; }

        /// <summary>
        /// Gets the default conflic resolution action that should be used when this constraint is violated.
        /// </summary>
        ConflictResolution ConflictResolution { get; }

        /// <summary>
        /// Checks if this constraint is violated.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        bool IsViolated(T a, T b);

        /// <summary>
        /// Gets the value of the property this constraint applies to.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        object GetPropertyValue(T entity);
    }
}
