using System;

namespace Moneyes.Data
{
    /// <summary>
    /// A unique database entity.
    /// </summary>
    public abstract class UniqueEntity
    {
        /// <summary>
        /// A unique identifier for this entity.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The date and time of creation.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The timestamp of the last database update.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets whether this entity is soft deleted.
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Returns true if <see cref="ID"/> equals <paramref name="other"/>.ID.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
#nullable enable
        public bool IdEquals(UniqueEntity? other)
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
        public abstract bool ContentEquals(UniqueEntity other);

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

        /// <summary>
        /// Creates a new instance of <see cref="UniqueEntity"/> from another <see cref="UniqueEntity"/>, 
        /// overriding the old parameters when given.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="id"></param>
        /// <param name="createdAt"></param>
        /// <param name="updatedAt"></param>
        /// <param name="isDeleted"></param>
        public UniqueEntity(UniqueEntity other, Guid? id = null, DateTime? createdAt = null, DateTime? updatedAt = null, bool? isDeleted = null)
        {
            ArgumentNullException.ThrowIfNull(other);

            Id = id ?? other.Id;
            CreatedAt = createdAt ?? other.CreatedAt;
            UpdatedAt = updatedAt ?? other.UpdatedAt;
            IsDeleted = isDeleted ?? other.IsDeleted;
        }
    }
}
