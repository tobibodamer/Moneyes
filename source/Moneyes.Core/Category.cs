using Moneyes.Core.Filters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.Core
{
    /// <summary>
    /// Represents a category for transactions.
    /// </summary>
    public class Category
    {
        public static readonly Guid NoCategoryId = new("38093a27-b7e4-4222-a7f7-9ca72c98544c");

        public static readonly Guid AllCategoryId = new("b52174ea-5869-4cbc-99f5-e48a274f9866");

        /// <summary>
        /// Gets a category indicating no category has been assoiated with a transaction.
        /// </summary>
        public static readonly Category NoCategory = new(NoCategoryId) { Name = "No category", IsExlusive = true };

        /// <summary>
        /// Gets a category all transactions belong to.
        /// </summary>
        public static readonly Category AllCategory = new(AllCategoryId) { Name = "All", IsExlusive = true };

        /// <summary>
        /// Gets the unique identifier of this category.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Name of the category.
        /// </summary>
        public string Name { get; set; }

#nullable enable
        /// <summary>
        /// A <see cref="TransactionFilter"/> used to identify transactions belonging to this category. <br></br>
        /// Can be <see langword="null"/> for dumb categories.
        /// </summary>
        public TransactionFilter? Filter { get; set; }

        /// <summary>
        /// The parent category.
        /// </summary>
        public Category? Parent { get; set; }
#nullable disable

        /// <summary>
        /// Gets or sets the monthly target amount for this category.
        /// </summary>
        public decimal Target { get; set; }

        /// <summary>
        /// Indicates whether transactions are exclusive to this category.
        /// </summary>
        public bool IsExlusive { get; set; }


        public bool IsReal => this.Id != NoCategory.Id && this.Id != AllCategory.Id;


        public Category(Guid id)
        {
            Id = id;
        }

        public override bool Equals(object obj)
        {
            return obj is Category category &&
                   Name == category.Name &&
                   Target == category.Target &&
                   Parent == category.Parent &&
                   IsExlusive == IsExlusive;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Filter, Target);
        }

        public static bool operator ==(Category left, Category right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is null ^ right is null)
            {
                return false;
            }

            return left.Id == right.Id;
        }

        public static bool operator !=(Category left, Category right)
        {
            return !(left == right);
        }
    }
}
