using Moneyes.Core;
using Moneyes.Core.Filters;
using System.Collections.Generic;

namespace Moneyes.UI
{
    public interface ITransactionService
    {
        bool AddToCategory(Transaction transaction, Category category);
        bool MoveToCategory(Transaction transaction, Category currentCategory, Category targetCategory);

        IEnumerable<Transaction> GetTransactions(Category category);
        IEnumerable<Transaction> GetTransactions(params Category[] categories);
        IEnumerable<Transaction> GetTransactions(TransactionFilter filter);
        IEnumerable<Transaction> GetTransactions(TransactionFilter filter, params Category[] categories);
    }
}