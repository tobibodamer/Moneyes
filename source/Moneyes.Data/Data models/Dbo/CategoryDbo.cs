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
    }
}
