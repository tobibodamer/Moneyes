using Moneyes.Core;

namespace Moneyes.UI
{
    public interface IExpenditure
    {
        /// <summary>
        /// Gets the total amount over this expenditure period.
        /// </summary>
        decimal TotalAmount { get; }

        /// <summary>
        /// Gets the total expense over this expenditure period.
        /// </summary>
        decimal TotalExpense { get; init; }

        /// <summary>
        /// Gets the total income over this expenditure period.
        /// </summary>
        decimal TotalIncome { get; init; }

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

        /// <summary>
        /// Gets whether the expense and income are absolute values.
        /// </summary>
        bool IsAbsolute { get; }

        /// <summary>
        /// Gets the expenses of each day.
        /// </summary>
        IReadOnlyDictionary<DateTime, IExpenditure> DailyExpenses { get; }

        /// <summary>
        /// Gets the expenses of each month.
        /// </summary>
        IReadOnlyDictionary<DateTime, IExpenditure> MonthlyExpenses { get; }

        /// <summary>
        /// Gets the expenses of each year.
        /// </summary>
        IReadOnlyDictionary<DateTime, IExpenditure> YearlyExpenses { get; }

        /// <summary>
        /// Gets the daily average expenditure.
        /// </summary>
        /// <param name="includeEmptyDays">Include days without expenses</param>
        /// <param name="limitToCurrentDate">Limit the end of the period to the current date, to leave out future days.</param>
        /// <returns>The daily average.</returns>
        decimal GetDailyAverage(TransactionType type = TransactionType.None, bool includeEmptyDays = true, bool limitToCurrentDate = true);

        /// <summary>
        /// Gets the monthly average expenditure.
        /// </summary>
        /// <param name="includeEmptyMonths">Include months without expenses</param>
        /// <param name="limitToCurrentMonth">Limit the end of the period to the current month, to leave out future months.</param>
        /// <returns>The monthly average.</returns>
        decimal GetMonthlyAverage(TransactionType type = TransactionType.None, bool includeEmptyMonths = true, bool limitToCurrentMonth = true);

        /// <summary>
        /// Gets the number of months in the expenditure period, including empty months (with no expenses).
        /// </summary>
        /// <param name="limitToCurrentMonth">Limit the end of the period to the current month, to leave out future months.</param>
        /// <returns>The total month span.</returns>
        int GetMonths(bool limitToCurrentMonth = true);

        /// <summary>
        /// Gets a sub expenditure range of this expenditure.
        /// </summary>
        /// <param name="startDate">The new start date. Must be greater than the current start date.</param>
        /// <param name="endDate">The new end date. Must be smaller than the current end date.</param>
        /// <returns>A new expenditure over the given period.</returns>
        IExpenditure SubRange(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Gets either the total income, expense or amount using the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of amount.</param>
        decimal GetAmountByType(TransactionType type);

        /// <summary>
        /// Gets the average amount over the given <paramref name="timePeriod"/>.
        /// </summary>
        decimal GetAverage(TimeSpan timePeriod, TransactionType type = TransactionType.None);
    }
}