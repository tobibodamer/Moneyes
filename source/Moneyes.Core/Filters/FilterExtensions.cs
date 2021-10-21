using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Moneyes.Core.Filters
{
    public static class FilterExtensions
    {
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

        public static string GetName<TSource, TField>(this Expression<Func<TSource, TField>> field)
        {
            return (field.Body as MemberExpression ?? 
                ((UnaryExpression)field.Body).Operand as MemberExpression).Member.Name;
        }

        public static string GetName<TSource, TField>(this Func<TSource, TField> expr)
        {
            return GetName<TSource, TField>(field: source => expr(source));
        }

        public static bool Evaluate(this TransactionFilter filter, Transaction transaction)
        {
            return (filter.TransactionType is TransactionType.None || MatchesSaleType(transaction, filter.TransactionType))
                && (!filter.StartDate.HasValue || (transaction.BookingDate >= filter.StartDate))
                && (!filter.EndDate.HasValue || (transaction.BookingDate <= filter.EndDate))
                && filter.Criteria.Evaluate(transaction);
        }

        private static bool MatchesSaleType(Transaction t, TransactionType? saleType)
        {
            if (!saleType.HasValue) { return true; }

            if ((saleType == TransactionType.Expense) && t.Amount >= 0)
            {
                return true;
            }
            else if ((saleType == TransactionType.Income) && t.Amount < 0)
            {
                return true;
            }

            return false;
        }
    }

}
