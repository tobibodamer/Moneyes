using System;

namespace Moneyes.Data
{
    /// <summary>
    /// A unique database entity.
    /// </summary>
    public abstract record UniqueEntity<T> 
        where T : UniqueEntity<T>
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
        public DateTime UpdatedAt { get; init; }

        /// <summary>
        /// Gets or sets whether this entity is soft deleted.
        /// </summary>
        public bool IsDeleted { get; init; }

        /// <summary>
        /// Returns true if <see cref="ID"/> equals <paramref name="other"/>.ID.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
#nullable enable
        public bool IdEquals(T? other)
#nullable disable
        {
            return this.Id.Equals(other?.Id);
        }

        /// <summary>
        /// Returns true if the content equals to the content of another entity, 
        /// even if the id, timestamps or <see cref="IsDeleted"/> property might differ.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public abstract bool ContentEquals(T other);

        /// <summary>
        /// For deserialization only.
        /// </summary>
        protected UniqueEntity() { }

        /// <summary>
        /// Creates a new instance of <see cref="UniqueEntity"/> with the given parameters.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="createdAt"></param>
        /// <param name="updatedAt"></param>
        /// <param name="isDeleted"></param>
        public UniqueEntity(Guid id, DateTime? createdAt = null, DateTime? updatedAt = null, bool isDeleted = false)
        {
            Id = id;
            CreatedAt = createdAt ?? DateTime.UtcNow;
            UpdatedAt = updatedAt ?? DateTime.UtcNow;
            IsDeleted = isDeleted;
        }
    }
}
