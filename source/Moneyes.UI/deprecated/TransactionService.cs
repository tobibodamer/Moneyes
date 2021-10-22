using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Moneyes.UI
{
    public class TransactionService : ITransactionService
    {
        private readonly IRepository<Transaction> _transactionStore;

        public event Action TransactionsChanged;

        public TransactionService(IRepository<Transaction> transactionStore)
        {
            _transactionStore = transactionStore;
        }

        public async Task<Result<IEnumerable<Transaction>>> GetTransactions(
            IEvaluable<Transaction> filter = null, params Category[] categories)
        {
            try
            {
                IEnumerable<Transaction> transactions = await _transactionStore.GetAll();
                IEnumerable<Transaction> filtered = transactions;
                bool hasNoCategory = false;

                // Remove null values, set to null if empty
                if (categories != null)
                {
                    categories = categories.Where(c => c != null).ToArray();

                    if (!categories.Any())
                    {
                        categories = null;
                    }
                    else if (categories.Any(c => c == Category.NoCategory))
                    {
                        hasNoCategory = true;
                    }
                }

                if (filter == null && categories == null)
                {
                    // No need to filter
                    return Result.Successful(transactions);
                }

                if (filter != null)
                {
                    // Apply filter
                    filtered = transactions.Where(transaction =>
                        filter.Evaluate(transaction));
                }

                if (categories != null)
                {
                    // Sort into category
                    filtered = filtered.Where(transaction =>
                    {
                        if (transaction.Categories != null
                            && transaction.Categories.Any())
                        {
                            return categories.Any(c =>
                                transaction.Categories.Contains(c));
                        }

                        return hasNoCategory;
                    });
                }

                return Result.Successful(filtered);
            }
            catch
            {
                return Result.Failed<IEnumerable<Transaction>>();
            }
        }

        public async Task<Result> Update(Transaction transaction)
        {
            try
            {
                await _transactionStore.SetItem(transaction);

                TransactionsChanged?.Invoke();

                return Result.Successful();
            }
            catch
            {
                return Result.Failed();
            }
        }
    }
}