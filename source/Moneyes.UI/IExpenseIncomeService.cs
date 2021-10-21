using Moneyes.Core;
using Moneyes.LiveData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moneyes.UI
{
    public interface IExpenseIncomeService
    {
        Result<IEnumerable<(Category Category, decimal TotalAmt)>> GetExpensePerCategory(AccountDetails account, bool includeOther = true);
        Result<decimal> GetTotalExpense(AccountDetails account);
        Result<decimal> GetTotalExpense(AccountDetails account, Category category);
        Result<decimal> GetTotalIncome(AccountDetails account);
        Result<decimal> GetTotalIncome(AccountDetails account, Category category);
    }
}