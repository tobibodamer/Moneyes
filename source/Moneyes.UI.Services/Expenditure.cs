using Moneyes.Core;
using System;
using System.Collections.Generic;

namespace Moneyes.UI
{
    public sealed class Expenditure : IExpenditure
    {
        public IReadOnlyList<Transaction> Transactions { get; init; } = new List<Transaction>();
        public decimal TotalAmount { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public TimeSpan ExpenditurePeriod => EndDate - StartDate;
    }
}
