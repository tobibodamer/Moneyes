using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Moneyes.Core.Filters
{
    public static class ConditionFilters
    {
        /// <summary>
        /// Creates a condition filter instance from a selector.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static IConditionFilter<T> Create<T>(string selector)
        {
            // Getting type of selector
            Type selectorType = typeof(T).GetProperty(selector).PropertyType;

            // Make generic condition filter type
            Type conditionFilterType = typeof(ConditionFilter<,>).MakeGenericType(typeof(T), selectorType);

            dynamic conditionFilter = Activator.CreateInstance(conditionFilterType);

            if (conditionFilter != null)
            {
                conditionFilter.Selector = selector;
            }

            return conditionFilter as IConditionFilter<T>;
        }
    }


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
        public List<TValue> Values { get; set; } = new List<TValue>();
        public ConditionOperator Operator { get; set; }
        public bool CaseSensitive { get; set; }
        public bool CompareAll { get; set; }

        #region Explicit implementations 
        IEnumerable IConditionFilter<T>.Values
        {
            get => Values;
            set
            {
                Values = new(value.Cast<TValue>());
            }
        }

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

            return Operator switch
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
