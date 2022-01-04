using Moneyes.Core;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moneyes.UI
{
    public interface ILiveDataService
    {
        event Action<OnlineBankingDetails> BankingInitialized;

        Task<Result> CreateBankConnection(OnlineBankingDetails bankingDetails, bool testConnection = false);
        Task<Result<IEnumerable<AccountDetails>>> FetchAccounts();
        Task<Result> FetchAndImportAccounts();
        Task<Result<int>> FetchTransactionsAndBalances(AccountDetails account, AssignMethod categoryAssignMethod = AssignMethod.KeepPrevious);
        Task<Result<int>> FetchTransactionsAndBalances(AccountDetails[] accounts, AssignMethod categoryAssignMethod = AssignMethod.KeepPrevious);
        IBankInstitute FindBank(int bankCode);
    }
}