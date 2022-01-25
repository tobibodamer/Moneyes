using System;

namespace Moneyes.Core
{
    /// <summary>
    /// A unique database entity.
    /// </summary>
    public class UniqueEntity
    {
        /// <summary>
        /// A unique identifier for this entity.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// The date and time of creation.
        /// </summary>
        public DateTime CreatedAt { get; init; }

        /// <summary>
        /// The timestamp of the last database update.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets whether this entity is soft deleted.
        /// </summary>
        public bool IsDeleted { get; set; }
    }
}
