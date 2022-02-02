namespace Moneyes.Data
{
    /// <summary>
    /// Options for a cached repository.
    /// </summary>
    public class CachedRepositoryOptions
    {
        /// <summary>
        /// Gets or sets whether the cache is preloaded during initialization.
        /// </summary>
        public bool PreloadCache { get; set; }

        /// <summary>
        /// Gets or sets the name of the underlying database collection.
        /// </summary>
        public string CollectionName { get; set; }
    }
}