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
        /// Checks if this constraint is violated.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        bool Allows(T a, T b);
    }
}
