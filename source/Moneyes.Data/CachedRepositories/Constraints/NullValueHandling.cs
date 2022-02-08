namespace Moneyes.Data
{
    /// <summary>
    /// Specifies how to handle null values of a unique index.
    /// </summary>
    public enum NullValueHandling
    {
        /// <summary>
        /// Null values will be ignored when validating the unique constraint.
        /// </summary>
        Ignore,

        /// <summary>
        /// Include null values when validating the unique constraint.
        /// </summary>
        Include
    }
}
