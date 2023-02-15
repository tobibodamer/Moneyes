namespace Moneyes.UI
{
    /// <summary>
    /// Describes the method used to assign categories to transactions.
    /// </summary>
    public enum AssignMethod
    {
        /// <summary>
        /// Assign matching categories to a transaction, overwriting the existing category.
        /// </summary>
        Simple,

        /// <summary>
        /// Assign matching categories to transactions without a category.
        /// </summary>
        KeepPrevious,

        /// <summary>
        /// Assign matching categories to new transactions only. 
        /// Transactions that are already imported are kept untouched.
        /// </summary>
        KeepPreviousAlways,

        /// <summary>
        /// Like <see cref="Simple"/>, but always resets the category beforehand. 
        /// Can result in transactions being removed from a category, if no matching category is found!
        /// </summary>
        Reset
    }
}
