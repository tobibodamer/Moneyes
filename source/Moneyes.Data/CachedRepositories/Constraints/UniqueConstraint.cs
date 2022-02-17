using Moneyes.Core.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public NullValueHandling NullValueHandling { get; }

        public UniqueConstraint(
            Expression<Func<T, K>> selector, 
            string collectionName,
            ConflictResolution conflictResolution = default, 
            NullValueHandling nullValueHandling = default)
        {
            Selector = selector.Compile();
            CollectionName = collectionName;
            PropertyName = GetPropertyName(selector);
            ConflictResolution = conflictResolution;
            NullValueHandling = nullValueHandling;
        }

        private static string GetPropertyName(Expression<Func<T, K>> selector)
        {
            if (selector.ReturnType.IsAnonymousType())
            {
                // Try to get a pretty print of the anonymous type

                // Get the length of the parameter (eg. 'input' in input => new { input.XXX, ... })
                var parameterLength = selector.Parameters[0].Name.Length;

                // Get the arguments of the lambda expression (e.g. 'input.XXX', ...)
                var arguments = (selector.Body as NewExpression).Arguments.Select(x => x.ToString().Substring(parameterLength + 1));

                return "{ " + string.Join(", ", arguments) + " }";
            }
            else
            {
                return FilterExtensions.GetName(selector);
            }
        }

        public bool IsViolated(T a, T b)
        {
            var _a = Selector(a);
            var _b = Selector(b);

            return EqualityComparer.Equals(_a, _b);
        }

        public object GetPropertyValue(T entity)
        {
            return Selector(entity);
        }

        public int? HashPropertyValue(T entity)
        {
            K value = Selector(entity);
            return value?.GetHashCode();
        }
    }
}
