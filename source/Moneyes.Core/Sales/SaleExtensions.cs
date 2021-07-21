using MoneyesParser.Filters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MoneyesParser
{
    /// <summary>
    /// Provides extension methods for <see cref="ISale"/>.
    /// </summary>
    public static class SaleExtensions
    {
        /// <summary>
        /// Apply a <see cref="SalesFilter"/> to a collection of <see cref="ISale"/>s.
        /// </summary>
        /// <param name="sales"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static IEnumerable<ISale> FilterSales(this IEnumerable<ISale> sales, SalesFilter filter)
        {
            var filteredSales = sales.Where(sale =>
                (!filter.SaleType.HasValue || sale.SaleType == filter.SaleType)
                && (!filter.StartDate.HasValue || (sale.BookingDate >= filter.StartDate))
                && (!filter.EndDate.HasValue || (sale.BookingDate <= filter.EndDate))
                && filter.Criteria.Evaluate(sale));

            return filteredSales;
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
