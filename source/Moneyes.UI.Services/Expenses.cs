using Moneyes.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Moneyes.UI
{
    public class Expenses : IExpenditure
    {
        /// <summary>
        /// Gets the expenses of each day.
        /// </summary>
        public IReadOnlyDictionary<DateTime, IExpenditure> DailyExpenses { get; } = new Dictionary<DateTime, IExpenditure>();

        /// <summary>
        /// Gets the expenses of each month.
        /// </summary>
        public IReadOnlyDictionary<DateTime, IExpenditure> MonthlyExpenses { get; } = new Dictionary<DateTime, IExpenditure>();

        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public TimeSpan ExpenditurePeriod => EndDate - StartDate;
        public decimal TotalAmount { get; init; }
        public IReadOnlyList<Transaction> Transactions { get; }

        public Expenses(IEnumerable<Transaction> transactions)
        {
            Transactions = transactions.ToList();

            if (!transactions.Any())
            {
                return;
            }

            var groupedByDay = transactions.GroupBy(t => t.BookingDate.Date);

            DailyExpenses = groupedByDay.ToDictionary(g => g.Key, g =>
                new Expenditure()
                {
                    Transactions = g.ToList(),
                    TotalAmount = Math.Abs(g.Sum(t => t.Amount)),
                    StartDate = g.Key,
                    EndDate = g.Key.AddDays(1)
                } as IExpenditure);

            var groupedByMonth = transactions.GroupBy(t => new DateTime
                (
                    t.BookingDate.Year,
                    t.BookingDate.Month,
                    1));

            MonthlyExpenses = groupedByMonth.ToDictionary(g => g.Key, g =>
                new Expenditure()
                {
                    Transactions = g.ToList(),
                    TotalAmount = Math.Abs(g.Sum(t => t.Amount)),
                    StartDate = g.Key,
                    EndDate = g.Key.AddMonths(1)
                } as IExpenditure);

            //StartDate = groupedByDay.Min(g => g.Key);
            //EndDate = groupedByDay.Max(g => g.Key);

            TotalAmount = MonthlyExpenses.Values.Sum(e => e.TotalAmount);
        }

        /// <summary>
        /// Gets the monthly average expenditure.
        /// </summary>
        /// <param name="includeEmptyMonths">Include months without expenses</param>
        /// <param name="limitToCurrentMonth">Limit the end of the period to the current month, to leave out future months.</param>
        /// <returns>The monthly average.</returns>
        public decimal GetMonthlyAverage(bool includeEmptyMonths = true, bool limitToCurrentMonth = true)
        {
            if (Transactions.Count == 0)
            {
                return 0;
            }

            int totalMonths = MonthlyExpenses.Count;

            if (includeEmptyMonths)
            {
                totalMonths = MonthDifference(EndDate, StartDate, limitToCurrentMonth) + 1;
            }

            return TotalAmount / totalMonths;
        }

        /// <summary>
        /// Gets the average expenditure amount over the given time period.
        /// </summary>
        /// <param name="timePeriod">The time period (e.g. 30 day average)</param>
        /// <returns>The average expense.</returns>
        public decimal GetAverage(TimeSpan timePeriod)
        {
            if (Transactions.Count == 0)
            {
                return 0;
            }

            int totalDays = (int)(EndDate - StartDate).TotalDays;
            decimal perDayAmt = TotalAmount / totalDays;

            return perDayAmt * (decimal)timePeriod.TotalDays;
        }

        public static int MonthDifference(DateTime lValue, DateTime rValue, bool limitToCurrentMonth = false)
        {
            if (limitToCurrentMonth)
            {
                lValue = new(Math.Min(DateTime.Now.Ticks, lValue.Ticks));
                rValue = new(Math.Min(DateTime.Now.Ticks, rValue.Ticks));
            }

            return Math.Abs((lValue.Month - rValue.Month) + 12 * (lValue.Year - rValue.Year));
        }
    }
}
