using Moneyes.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moneyes.LiveData
{
    public interface IOnlineBankingService
    {
        OnlineBankingDetails BankingDetails { get; }

        Task<Result<IEnumerable<AccountDetails>>> Accounts();
        Task<Result<Balance>> Balance(AccountDetails account);
        Task Sync();
        Task<Result<TransactionData>> Transactions(AccountDetails account, DateTime? startDate = null, DateTime? endDate = null);
    }
}