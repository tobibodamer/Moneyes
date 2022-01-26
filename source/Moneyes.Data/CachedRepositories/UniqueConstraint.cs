using Moneyes.Core.Filters;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Moneyes.Data
{
    internal class UniqueConstraint<T, K> : IUniqueConstraint<T>
    {
        public Func<T, K> Selector { get; }

        public string CollectionName { get; }

        public string PropertyName { get; }

        public IEqualityComparer<K> EqualityComparer { get; set; } = EqualityComparer<K>.Default;

        public UniqueConstraint(Expression<Func<T, K>> selector, string collectionName)
        {
            Selector = selector.Compile();
            CollectionName = collectionName;
            PropertyName = FilterExtensions.GetName(selector);
        }

        public bool Allows(T a, T b)
        {
            return EqualityComparer.Equals(Selector(a), Selector(b));
        }
    }
}
