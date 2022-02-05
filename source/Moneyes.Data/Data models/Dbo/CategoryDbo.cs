using Moneyes.Core.Filters;
using System;

namespace Moneyes.Data
{
    public class CategoryDbo : UniqueEntity
    {
        /// <summary>
        /// Name of the category.
        /// </summary>
        public string Name { get; set; }

#nullable enable
        /// <summary>
        /// A <see cref="TransactionFilter"/> used to identify transactions belonging to this category. <br></br>
        /// Can be <see langword="null"/> for dumb categories.
        /// </summary>
        public TransactionFilterDto? Filter { get; set; }

        /// <summary>
        /// The parent category.
        /// </summary>
        public CategoryDbo? Parent { get; set; }
#nullable disable

        /// <summary>
        /// Gets or sets the monthly target amount for this category.
        /// </summary>
        public decimal Target { get; set; }

        /// <summary>
        /// Indicates whether transactions are exclusive to this category.
        /// </summary>
        public bool IsExlusive { get; set; }

        public override bool ContentEquals(UniqueEntity other)
        {
            return other is CategoryDbo otherCategory &&
                Name == otherCategory.Name &&
                (Filter?.Equals(otherCategory.Filter) ?? otherCategory.Filter is null) &&
                (Parent?.IdEquals(otherCategory.Parent) ?? otherCategory.Parent is null) &&
                Target == otherCategory.Target &&
                IsExlusive == otherCategory.IsExlusive;
        }

        public override bool Equals(object obj)
        {
            return obj is CategoryDbo category &&
                   Name == category.Name &&
                   Target == category.Target &&
                   Parent == category.Parent &&
                   IsExlusive == IsExlusive;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Filter, Target);
        }

        /// <summary>
        /// For deserialization only.
        /// </summary>
        protected CategoryDbo() { }

        public CategoryDbo(
            Guid id,
            string name,
            DateTime? createdAt = null,
            DateTime? updatedAt = null,
            bool isDeleted = false)
            : base(id, createdAt, updatedAt, isDeleted)
        {
            Name = name;
        }

        public CategoryDbo(
            CategoryDbo other,
            string name = null,
            Guid? id = null,
            DateTime? createdAt = null,
            DateTime? updatedAt = null,
            bool? isDeleted = null)
            : base(other, id, createdAt, updatedAt, isDeleted)
        {
            Name = name ?? other.Name;
        }
    }
}
