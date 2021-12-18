using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Moneyes.Core.Filters
{
    /// <summary>
    /// Represents an evaluable group of filters.
    /// </summary>
    /// <typeparam name="T">The type of the filtered object.</typeparam>
    public class FilterGroup<T> : IEvaluable<T>
    {
        /// <summary>
        /// Gets or sets the logical operator used to aggregate the evaluated conditions and child filters. <br></br>
        /// Default value is <see cref="LogicalOperator.And"/>.
        /// </summary>
        public LogicalOperator Operator { get; set; } = LogicalOperator.And;

        /// <summary>
        /// A list of conditions contained in this filter group.
        /// </summary>
        public List<IConditionFilter<T>> Conditions { get; init; } = new List<IConditionFilter<T>>();

        /// <summary>
        /// A list of child filters used to chain multiple filters with different logical operators.
        /// </summary>
        public List<FilterGroup<T>> ChildFilters { get; init; } = new();

        public FilterGroup() { }
        public FilterGroup(LogicalOperator logicalOperator)
        {
            Operator = logicalOperator;
        }

        /// <summary>
        /// Adds a <see cref="IConditionFilter{T}"/> to this filter group.
        /// </summary>
        /// <typeparam name="TValue">The type of the selected property.</typeparam>
        /// <param name="selector">The selector used to target the property.</param>
        /// <param name="conditionOperator">The condition operator to use.</param>
        /// <param name="values">The values used by the condition.</param>
        /// <returns></returns>
        public ConditionFilter<T, TValue> AddCondition<TValue>(Expression<Func<T, TValue>> selector,
            ConditionOperator conditionOperator, params TValue[] values)
        {
            var conditionFilter = new ConditionFilter<T, TValue>()
            {
                Selector = selector.GetName(),
                Operator = conditionOperator,
                Values = values.ToList()
            };
            
            Conditions.Add(conditionFilter);

            return conditionFilter;
        }

        /// <summary>
        /// Adds a <see cref="IConditionFilter{T}"/> to this filter group.
        /// </summary>
        public void AddCondition(IConditionFilter<T> conditionFilter)
        {
            Conditions.Add(conditionFilter);
        }

        /// <summary>
        /// Adds a child <see cref="FilterGroup{T}"/> to this filter group.
        /// </summary>
        /// <param name="logicalOperator">The logical operator of the child filter.</param>
        /// <returns></returns>
        public FilterGroup<T> AddFilter(LogicalOperator logicalOperator)
        {
            FilterGroup<T> filterGroup = new(logicalOperator);

            ChildFilters.Add(filterGroup);

            return filterGroup;
        }

        /// <summary>
        /// Evaluates the filter group for a given input, by recursively evaluating the conditions 
        /// and child filters and aggregating them with the <see cref="LogicalOperator"/>.
        /// </summary>
        /// <param name="input">The input object to evaluate this filter for.</param>
        /// <returns><c>true</c> if the input satisfies the filter, <c>false</c> otherwise.</returns>
        public bool Evaluate(T input)
        {
            if (!Conditions.Any() && !ChildFilters.Any())
            {
                return true;
            }

            bool conditionEvaluation = Conditions.EvaluateFilters(input, Operator);
            bool childFilterEvaluation = ChildFilters.EvaluateFilters(input, Operator);

            return Operator switch
            {
                LogicalOperator.And =>
                    conditionEvaluation && childFilterEvaluation,
                _ =>
                    conditionEvaluation || childFilterEvaluation
            };
        }
    }

}
