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
        /// Get total expenditure of a given <paramref name="category"/> and <paramref name="filter"/>.
        /// Offsets expenses and income amounts.
        /// </summary>
        /// <param name="category">The category of the transactions.</param>
        /// <param name="filter">The filter applied to the transactions.</param>
        /// <param name="includeSubCategories">Include transactions of sub categories of the given category.</param>
        /// <param name="absolute">Calculate the absolute summed up amount.</param>
        /// <returns></returns>
        Result<IExpenditure> GetExpenditure(
            Category category, TransactionFilter filter, bool includeSubCategories = false, bool absolute = false);

        /// <summary>
        /// Get all income and expense transactions summed up, and their associated categories.
        /// </summary>        
        /// <param name="filter">The filter applied to the transactions.</param>
        /// <param name="categories">The category types to include.</param>
        /// <param name="includeSubCategories">Include transactions of sub categories of the given category.</param>
        /// <param name="absolute">Calculate the absolute summed up amount.</param>
        /// <returns></returns>
        Result<IEnumerable<(Category Category, IExpenditure Expenses)>> GetExpenditurePerCategory(
            TransactionFilter filter,
            CategoryTypes categories = CategoryTypes.Real | CategoryTypes.NoCategory,
            bool includeSubCategories = false,
            bool absolute = false);
        Result<IExpenditure> GetTotalExpenditure(TransactionFilter filter, bool absolute = true);
    }
}