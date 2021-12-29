using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Moneyes.Core.Filters
{
    public static class FilterExtensions
    {
        /// <summary>
        /// Evaluates multiple <paramref name="filters"/> for a given <paramref name="input"/>, 
        /// by combining them with the given <paramref name="logicalOperator"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filters"></param>
        /// <param name="input"></param>
        /// <param name="logicalOperator"></param>
        /// <returns></returns>
        public static bool EvaluateFilters<T>(this IEnumerable<IEvaluable<T>> filters,
            T input, LogicalOperator logicalOperator)
        {
            if (!filters.Any()) { return logicalOperator == LogicalOperator.And; }

            bool expression = filters.First().Evaluate(input);

            foreach (var condition in filters.Skip(1))
            {
                expression = logicalOperator switch
                {
                    LogicalOperator.And =>
                        expression && condition.Evaluate(input),
                    _ =>
                        expression || condition.Evaluate(input)
                };
            }

            return expression;
        }

        /// <summary>
        /// Returns all values that evaluate positive with the given filter.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static IEnumerable<T> Filter<T>(this IEnumerable<T> values, IEvaluable<T> filter)
        {
            return values.Where(value => filter.Evaluate(value));
        }

        /// <summary>
        /// Gets the target member name of a selector expression.
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TField">Target member type</typeparam>
        /// <param name="field">The selector expression</param>
        /// <returns></returns>
        internal static string GetName<TSource, TField>(this Expression<Func<TSource, TField>> field)
        {
            return (field.Body as MemberExpression ??
                ((UnaryExpression)field.Body).Operand as MemberExpression).Member.Name;
        }

        /// <summary>
        /// Gets the target member name of a selector expression.
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TField">Target member type</typeparam>
        /// <param name="field">The selector expression</param>
        /// <returns></returns>
        internal static string GetName<TSource, TField>(this Func<TSource, TField> expr)
        {
            return GetName<TSource, TField>(field: source => expr(source));
        }
    }

}
