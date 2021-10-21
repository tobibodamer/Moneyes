using Moneyes.Core.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.Core
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TransactionFilter Filter { get; set; }
        public decimal Target { get; set; }
        public Category Parent { get; set; }
        public bool IsExlusive { get; set; }

        public static readonly Category NoCategory = new() { Name = new Guid().ToString() };

        public override bool Equals(object obj)
        {
            return obj is Category category &&
                   Name == category.Name &&
                   EqualityComparer<TransactionFilter>.Default.Equals(Filter, category.Filter) &&
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
