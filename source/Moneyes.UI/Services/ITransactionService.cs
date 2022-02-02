using Moneyes.Core;
using Moneyes.Core.Filters;
using System;
using System.Collections.Generic;

namespace Moneyes.UI
{
    public interface ITransactionService
    {
        IEnumerable<Transaction> All(params Category[] categories);
        IEnumerable<Transaction> All(TransactionFilter filter);
        IEnumerable<Transaction> All(TransactionFilter filter, params Category[] categories);
        IEnumerable<Transaction> AllOrderedByDate();
        DateTime EarliestTransactionDate(TransactionFilter filter);
        IReadOnlyList<Transaction> GetAllTransactions();
        IEnumerable<Transaction> GetByCategory(Category category);
        Transaction GetByUID(string uid);
        bool ImportTransaction(Transaction transaction);
        int ImportTransactions(IEnumerable<Transaction> transactions);
        DateTime LatestTransactionDate(TransactionFilter filter);
    }
}