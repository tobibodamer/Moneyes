using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.LiveData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moneyes.UI
{
    public interface IExpenseIncomeService
    {
        /// <summary>
        /// Get all expenses of a given <paramref name="category"/> and <paramref name="filter"/>.
        /// </summary>
        /// <param name="category">The category of the expenses.</param>
        /// <param name="filter">The filter applied to the transactions.</param>
        /// <param name="includeSubCategories">Include sub categories of the given category.</param>
        /// <returns></returns>
        Result<Expenses> GetExpenses(
            Category category, TransactionFilter filter, bool includeSubCategories = false);

        /// <summary>
        /// Get all income of a given <paramref name="category"/> and <paramref name="filter"/>.
        /// </summary>
        /// <param name="category">The category of the transactions.</param>
        /// <param name="filter">The filter applied to the transactions.</param>
        /// <param name="includeSubCategories">Include sub categories of the given category.</param>
        /// <returns></returns>
        Result<Expenses> GetIncome(
            Category category, TransactionFilter filter, bool includeSubCategories = false);

        /// <summary>
        /// Get all expenses and associated categories.
        /// </summary>        
        /// <param name="filter">The filter applied to the transactions.</param>
        /// <param name="categories">The category types to include.</param>
        /// <param name="includeSubCategories">Include sub categories of the given category.</param>
        /// <returns></returns>
        Result<IEnumerable<(Category Category, Expenses Expenses)>> GetAllExpenses(
            TransactionFilter filter, CategoryTypes categories,
            bool includeSubCategories = false);

        /// <summary>
        /// Get all income and associated categories.
        /// </summary>        
        /// <param name="filter">The filter applied to the transactions.</param>
        /// <param name="categories">The category types to include.</param>
        /// <param name="includeSubCategories">Include sub categories of the given category.</param>
        /// <returns></returns>
        Result<IEnumerable<(Category Category, Expenses Expenses)>> GetAllIncome(
            TransactionFilter filter, CategoryTypes categories,
            bool includeSubCategories = false);

        Result<Expenses> GetTotalExpense(TransactionFilter filter);
        Result<Expenses> GetTotalIncome(TransactionFilter filter);
    }
}