using Moneyes.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moneyes.LiveData
{
    public interface IOnlineBankingService
    {
        OnlineBankingDetails BankingDetails { get; }
        Task<BankingResult<IEnumerable<AccountDetails>>> Accounts(BankDetails bank);
        Task<BankingResult<Balance>> Balance(AccountDetails account);
        Task<BankingResult> Sync();
        Task<BankingResult<TransactionData>> Transactions(AccountDetails account, DateTime? startDate = null, DateTime? endDate = null);
    }
}