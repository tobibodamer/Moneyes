using Moneyes.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moneyes.LiveData
{
    public interface IOnlineBankingService
    {
        Task<BankingResult> Sync(OnlineBankingDetails onlineBankingDetails);
        Task<BankingResult<IEnumerable<AccountDetails>>> Accounts(
            OnlineBankingDetails onlineBankingDetails, 
            BankDetails bank);
        Task<BankingResult<Balance>> Balance(
            OnlineBankingDetails onlineBankingDetails, 
            AccountDetails account);
        Task<BankingResult<TransactionData>> Transactions(
            OnlineBankingDetails onlineBankingDetails, 
            AccountDetails account, 
            DateTime? startDate = null, 
            DateTime? endDate = null);
        bool CanFetchTransactions(AccountDetails account);
    }
}