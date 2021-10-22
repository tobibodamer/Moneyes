using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Moneyes.Core.Filters
{
    /// <summary>
    /// Implementation of a conditional filter for objects of type <typeparamref name="T"/>,
    /// filtered by a value of type <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class ConditionFilter<T, TValue> : IConditionFilter<T>
    {
        private string _selectorName;
        private Func<T, TValue> _selector;
        public string Selector
        {
            get
            {
                return _selectorName;
            }
            set
            {
                var parameterExpression = Expression.Parameter(typeof(T), "input");
                var propExpression = Expression.Property(parameterExpression, value);

                var lambdaExpression = Expression
                    .Lambda<Func<T, TValue>>(propExpression, parameterExpression);

                _selector = lambdaExpression.Compile();

                _selectorName = value;
            }
        }

        /// <summary>
        /// The values used for the condition.
        /// </summary>
        public List<TValue> Values { get; init; } = new List<TValue>();
        public ConditionOperator ConditionOperator { get; init; }
        public bool CaseSensitive { get; set; }
        public bool CompareAll { get; set; }

        #region Explicit implementations 
        IEnumerable IConditionFilter<T>.Values => Values;

        #endregion
        public bool Evaluate(T input)
        {
            TValue target = _selector.Invoke(input);
            var stringTarget = CaseSensitive ? target as string : (target as string)?.ToLower();

            Func<Func<TValue, bool>, bool> valueSelector;

            if (CompareAll)
            {
                valueSelector = new Func<Func<TValue, bool>, bool>(Values.All);
            }
            else
            {
                valueSelector = new Func<Func<TValue, bool>, bool>(Values.Any);
            }

            return ConditionOperator switch
            {
                ConditionOperator.Equal => valueSelector(value => value.Equals(target)),
                ConditionOperator.Greater => valueSelector(value => target is IComparable comparableTarget && comparableTarget.CompareTo(value) > 0),
                ConditionOperator.Smaller => valueSelector(value => target is IComparable comparableTarget && comparableTarget.CompareTo(value) < 0),
                ConditionOperator.GreaterOrEqual => valueSelector(value => target is IComparable comparableTarget && comparableTarget.CompareTo(value) >= 0),
                ConditionOperator.SmallerOrEqual => valueSelector(value => target is IComparable comparableTarget && comparableTarget.CompareTo(value) <= 0),
                ConditionOperator.NotEqual => valueSelector(value => !value.Equals(target)),
                ConditionOperator.BeginsWith => stringTarget is not null &&
                    valueSelector(value => stringTarget.StartsWith(CaseSensitive ? value as string : (value as string).ToLower())),
                ConditionOperator.EndsWith => stringTarget is not null &&
                    valueSelector(value => stringTarget.EndsWith(CaseSensitive ? value as string : (value as string).ToLower())),
                ConditionOperator.Contains => stringTarget is not null &&
                    valueSelector(value => stringTarget.Contains(CaseSensitive ? value as string : (value as string).ToLower())),
                ConditionOperator.DoesNotContain => stringTarget is not null &&
                    valueSelector(value => !stringTarget.Contains(CaseSensitive ? value as string : (value as string).ToLower())),
                _ => false
            };
        }

        /// <summary>
        /// Sets the selector expression.
        /// </summary>
        /// <param name="selector"></param>
        public void SetSelector(Func<T, TValue> selector)
        {
            _selector = selector;
            _selectorName = selector.GetName();
        }
    }
}
