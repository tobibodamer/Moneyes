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
        IEnumerable<AccountDetails> GetAccounts();
        Balance GetBalance(DateTime date, AccountDetails account);
    }
}