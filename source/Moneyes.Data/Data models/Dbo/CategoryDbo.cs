using Moneyes.Core.Filters;
using System;

namespace Moneyes.Data
{
    public record CategoryDbo : UniqueEntity<CategoryDbo>
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
        public TransactionFilterDto? Filter { get; init; }

        /// <summary>
        /// The parent category.
        /// </summary>
        public CategoryDbo? Parent { get; set; }
#nullable disable

        /// <summary>
        /// Gets or sets the monthly target amount for this category.
        /// </summary>
        public decimal Target { get; init; }

        /// <summary>
        /// Indicates whether transactions are exclusive to this category.
        /// </summary>
        public bool IsExlusive { get; init; }

        public override bool ContentEquals(CategoryDbo otherCategory)
        {
            return
                Name == otherCategory.Name &&
                (Filter?.Equals(otherCategory.Filter) ?? otherCategory.Filter is null) &&
                (Parent?.IdEquals(otherCategory.Parent) ?? otherCategory.Parent is null) &&
                Target == otherCategory.Target &&
                IsExlusive == otherCategory.IsExlusive;
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
    }
}
