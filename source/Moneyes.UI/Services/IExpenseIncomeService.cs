using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.LiveData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moneyes.UI
{
    public interface IExpenseIncomeService
    {
        Result<IEnumerable<(Category Category, decimal TotalAmt)>> GetExpensePerCategory(
            AccountDetails account,
            bool includeNoCategory = true);
        Result<IEnumerable<(Category Category, decimal TotalAmt)>> GetExpensePerCategory(
            TransactionFilter filter, bool includeNoCategory = true);
        Result<decimal> GetTotalExpense(TransactionFilter filter);
        Result<decimal> GetTotalExpense(TransactionFilter filter, Category category);
        Result<decimal> GetTotalIncome(TransactionFilter filter);
        Result<decimal> GetTotalIncome(TransactionFilter filter, Category category);
    }
}