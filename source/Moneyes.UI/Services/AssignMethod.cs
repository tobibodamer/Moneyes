namespace Moneyes.UI
{
    /// <summary>
    /// Describes the method used to assign categories to transactions.
    /// </summary>
    public enum AssignMethod
    {
        /// <summary>
        /// Assigns only all matching categories, don't care about existing transactions.
        /// </summary>
        Simple,
        /// <summary>
        /// Assign new matching categories and merge with existing categories.
        /// </summary>
        Merge,
        /// <summary>
        /// Assign categories to new transactions only.
        /// </summary>
        KeepPrevious,
        /// <summary>
        /// Resets and reassigns all categories.
        /// </summary>
        Reset
    }
}
