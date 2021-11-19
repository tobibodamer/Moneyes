using Moneyes.Core;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;

namespace Moneyes.UI
{
    public interface IBankingService
    {
        bool HasBankingDetails { get; }
        OnlineBankingDetails BankingDetails { get; set; }

        event Action NewAccountsImported;

        IEnumerable<AccountDetails> GetAccounts();
        int ImportAccounts(IEnumerable<AccountDetails> accounts);
        Balance GetBalance(DateTime date, AccountDetails account);
    }
}