using Moneyes.Core;
using System;
using System.Collections.Generic;

namespace Moneyes.UI
{
    public interface IExpenditure
    {
        /// <summary>
        /// Gets the total amount over this expenditure period.
        /// </summary>
        decimal TotalAmount { get; }

        /// <summary>
        /// Gets all the transactions of this expenditure.
        /// </summary>
        IReadOnlyList<Transaction> Transactions { get; }

        /// <summary>
        /// Gets the inclusive start date of the expenditure period.
        /// </summary>
        DateTime StartDate { get; }

        /// <summary>
        /// Gets the inclusive end date of the expenditure period.
        /// </summary>
        DateTime EndDate { get; }

        /// <summary>
        /// Gets the expenditure period associated with this expenditure.
        /// </summary>
        TimeSpan ExpenditurePeriod { get; }
    }
}
