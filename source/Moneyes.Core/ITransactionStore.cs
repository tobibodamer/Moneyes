using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moneyes.Core
{
    public interface ITransactionStore
    {
        Task<IEnumerable<Transaction>> Store(IEnumerable<Transaction> transactions);
        Task<IEnumerable<Transaction>> Load();
    }
}
