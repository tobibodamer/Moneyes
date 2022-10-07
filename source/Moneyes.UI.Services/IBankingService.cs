using Moneyes.Core;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;

namespace Moneyes.UI
{
    /// <summary>
    /// Provides methods to access banking related data
    /// (such as accounts, balances or online banking details).
    /// </summary>
    public interface IBankingService
    {
        /// <summary>
        /// Raised when new accounts are imported.
        /// </summary>
        event Action NewAccountsImported;

        void AddBankConnection(BankDetails bankDetails);

        void UpdateBankConnection(BankDetails bankDetails);

        IReadOnlyList<BankDetails> GetBankEntries();

        /// <summary>
        /// Gets all accounts available for the current banking details.
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<AccountDetails> GetAccounts(BankDetails bankDetails);

        /// <summary>
        /// Gets all accounts available.
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<AccountDetails> GetAllAccounts();

        decimal GetOverallBalance(DateTime date);

        /// <summary>
        /// Gets the balance most recent to the given <paramref name="date"/>, 
        /// for a specific <paramref name="account"/>.
        /// </summary>
        /// <param name="date"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        Balance? GetBalance(DateTime date, AccountDetails account);

        /// <summary>
        /// Imports the given accounts.
        /// </summary>
        /// <param name="accounts"></param>
        /// <returns></returns>
        int ImportAccounts(IEnumerable<AccountDetails> accounts);

        /// <summary>
        /// Imports the given transactions and assigns them to categories.
        /// </summary>
        /// <param name="transactions">The transactions to import.</param>
        /// <param name="categoryAssignMethod">The category assign method to use.</param>
        /// <returns></returns>
        int ImportTransactions(IEnumerable<Transaction> transactions, AssignMethod categoryAssignMethod);

        /// <summary>
        /// Imports the given balances.
        /// </summary>
        /// <param name="balances">The balances to import.</param>
        /// <returns></returns>
        int ImportBalances(IEnumerable<Balance> balances);
    }
}