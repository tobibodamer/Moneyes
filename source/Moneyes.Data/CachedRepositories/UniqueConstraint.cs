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

        public ConflictResolution ConflictResolution { get; }

        public UniqueConstraint(Expression<Func<T, K>> selector, string collectionName, 
            ConflictResolution conflictResolution = default)
        {
            Selector = selector.Compile();
            CollectionName = collectionName;
            PropertyName = FilterExtensions.GetName(selector);
            ConflictResolution = conflictResolution;
        }

        public bool Allows(T a, T b)
        {
            var _a = Selector(a);
            var _b = Selector(b);

            return !EqualityComparer.Equals(_a, _b);
        }

        public object GetPropertyValue(T entity)
        {
            return Selector(entity);
        }
    }
}
