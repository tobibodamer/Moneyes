using Moneyes.Core;
using System;
using System.Collections.Generic;

namespace Moneyes.UI
{
    internal class BaseExpenditure
    {
        public IReadOnlyList<Transaction> Transactions { get; }
        public decimal TotalAmount { get; init; }
        public decimal TotalIncome { get; init; }
        public decimal TotalExpense { get; init; }

        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public TimeSpan ExpenditurePeriod => EndDate - StartDate;
        public bool IsAbsolute { get; }

        public BaseExpenditure(IEnumerable<Transaction> transactions, bool absolute)
        {
            Transactions = transactions.ToList().AsReadOnly();
            IsAbsolute = absolute;
        }

        public decimal GetAverage(TimeSpan timePeriod, TransactionType type = TransactionType.None)
        {
            if (Transactions.Count == 0)
            {
                return 0;
            }

            long totalDays = (int)(EndDate - StartDate).Days;
            decimal perDayAmt = GetAmountByType(type) / totalDays;

            return (decimal)((double)perDayAmt * timePeriod.TotalDays);
        }

        public decimal GetAmountByType(TransactionType type)
        {
            return type switch
            {
                TransactionType.Expense => TotalExpense,
                TransactionType.Income => TotalIncome,
                _ => TotalAmount
            };
        }

        public override string ToString()
        {
            return $"+{Math.Abs(TotalExpense)} | -{Math.Abs(TotalIncome)} ({Transactions.Count})";
        }
    }
}
