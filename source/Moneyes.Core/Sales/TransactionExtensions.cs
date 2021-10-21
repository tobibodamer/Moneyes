using Moneyes.Core.Filters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Moneyes.Core
{
    /// <summary>
    /// Provides extension methods for <see cref="Transaction"/>s.
    /// </summary>
    public static class TransactionExtensions
    {
        /// <summary>
        /// Apply a <see cref="TransactionFilter"/> to a collection of <see cref="ISale"/>s.
        /// </summary>
        /// <param name="transcations"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static IEnumerable<Transaction> FilterTransactions(this IEnumerable<Transaction> transcations,
            TransactionFilter filter)
        {
            var filteredSales = transcations.Where(t =>
                (filter.TransactionType is TransactionType.None || MatchesSaleType(t, filter.TransactionType))
                && (!filter.StartDate.HasValue || (t.BookingDate >= filter.StartDate))
                && (!filter.EndDate.HasValue || (t.BookingDate <= filter.EndDate))
                && filter.Criteria.Evaluate(t));

            return filteredSales;
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

        public static decimal CalculateTotalAmount(this IEnumerable<ISale> sales)
        {
            if (!sales.Any()) { return 0; }

            return Math.Abs(sales.Sum(sale => sale.Amount));
        }

        public static decimal CalulateAverageAmount(this IEnumerable<ISale> sales, int daysCount, int avgDays = 30)
        {
            if (!sales.Any()) { return 0; }

            decimal totalAmt = CalculateTotalAmount(sales);

            return CalculateAverage(totalAmt, daysCount, avgDays);
        }

        public static decimal CalulateAverageAmount(this IEnumerable<ISale> sales, int avgDays = 30)
        {
            if (!sales.Any()) { return 0; }

            var startDate = sales.Min(sale => sale.BookingDate);
            var endDate = sales.Max(sale => sale.BookingDate);
            int daysCount = (int)(endDate - startDate).TotalDays + 1;

            return CalulateAverageAmount(sales, daysCount, avgDays);
        }

        private static decimal CalculateAverage(decimal totalAmt, int daysCount, int avgDays)
        {
            decimal perDayAmt = totalAmt / daysCount;

            return perDayAmt * avgDays;
        }

        public static (decimal total, decimal avg) CalulateTotalAndAverageAmount(
            this IEnumerable<ISale> sales, int daysCount = int.MaxValue, int avgDays = 30)
        {
            if (!sales.Any()) { return (0, 0); }

            if (daysCount == int.MaxValue)
            {
                var startDate = sales.FindStartDate();
                var endDate = sales.FindEndDate();
                daysCount = (int)(endDate - startDate).TotalDays + 1;
            }

            decimal totalAmt = CalculateTotalAmount(sales);
            decimal avgAmt = CalculateAverage(totalAmt, daysCount, avgDays);

            return (totalAmt, avgAmt);
        }

        public static DateTime FindStartDate(this IEnumerable<ISale> sales)
        {
            if (!sales.Any()) { return default; }

            return sales.Min(sale => sale.BookingDate);
        }

        public static DateTime FindEndDate(this IEnumerable<ISale> sales)
        {
            if (!sales.Any()) { return default; }

            return sales.Max(sale => sale.BookingDate);
        }
    }


}
