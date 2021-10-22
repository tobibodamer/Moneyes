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
        /// Calculates the total amount of all given transactions.
        /// </summary>
        /// <param name="transactions"></param>
        /// <returns></returns>
        public static decimal CalculateTotalAmount(this IEnumerable<Transaction> transactions)
        {
            if (!transactions.Any()) { return 0; }

            return Math.Abs(transactions.Sum(t => t.Amount));
        }

        /// <summary>
        /// Calculates the average amount over the time span given by <paramref name="daysCount"/>, per <paramref name="avgDays"/>.
        /// </summary>
        /// <param name="transactions"></param>
        /// <param name="daysCount"></param>
        /// <param name="avgDays"></param>
        /// <returns></returns>
        public static decimal CalulateAverageAmount(this IEnumerable<Transaction> transactions, int daysCount, int avgDays = 30)
        {
            if (!transactions.Any()) { return 0; }

            decimal totalAmt = CalculateTotalAmount(transactions);

            return CalculateAverage(totalAmt, daysCount, avgDays);
        }
        

        /// <summary>
        /// Calculates the average amount over the time span of the given transactions, per <paramref name="avgDays"/>.
        /// </summary>
        /// <param name="transactions"></param>
        /// <param name="avgDays"></param>
        /// <returns></returns>
        public static decimal CalulateAverageAmount(this IEnumerable<Transaction> transactions, int avgDays = 30)
        {
            if (!transactions.Any()) { return 0; }

            DateTime startDate = transactions.FindStartDate();
            DateTime endDate = transactions.FindEndDate();
            int daysCount = (int)(endDate - startDate).TotalDays + 1;

            return CalulateAverageAmount(transactions, daysCount, avgDays);
        }

        private static decimal CalculateAverage(decimal totalAmt, int daysCount, int avgDays)
        {
            decimal perDayAmt = totalAmt / daysCount;

            return perDayAmt * avgDays;
        }

        /// <summary>
        /// Finds the earliest booking date of the given transactions.
        /// </summary>
        /// <param name="transactions"></param>
        /// <returns></returns>
        public static DateTime FindStartDate(this IEnumerable<Transaction> transactions)
        {
            if (!transactions.Any()) { return default; }

            return transactions.Min(sale => sale.BookingDate);
        }

        /// <summary>
        /// Finds the latest booking date of the given transactions.
        /// </summary>
        /// <param name="transactions"></param>
        /// <returns></returns>
        public static DateTime FindEndDate(this IEnumerable<Transaction> transactions)
        {
            if (!transactions.Any()) { return default; }

            return transactions.Max(sale => sale.BookingDate);
        }
    }


}
