using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.LiveData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moneyes.UI
{
    public interface ITransactionService
    {
        Task<Result<IEnumerable<Transaction>>> GetTransactions(
            IEvaluable<Transaction> filter = null, params Category[] categories);
    }
}
