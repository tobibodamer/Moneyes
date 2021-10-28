using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using System.Collections.Generic;

namespace Moneyes.UI
{

    class TransactionService : ITransactionService
    {
        private readonly TransactionRepository _transactionRepository;

        public TransactionService(TransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public IEnumerable<Transaction> GetTransactions(Category category)
        {
            return _transactionRepository.GetByCategory(category);
        }

        public IEnumerable<Transaction> GetTransactions(params Category[] categories)
        {
            return _transactionRepository.All(categories);
        }

        public IEnumerable<Transaction> GetTransactions(TransactionFilter filter)
        {
            return _transactionRepository.All(filter);
        }

        public IEnumerable<Transaction> GetTransactions(TransactionFilter filter, params Category[] categories)
        {
            return _transactionRepository.All(filter, categories);
        }

        public bool AddToCategory(Transaction transaction, Category category)
        {
            return MoveToCategory(transaction, null, category);
        }
        public bool MoveToCategory(Transaction transaction, Category currentCategory, Category targetCategory)
        {
            if (targetCategory == null || targetCategory == Category.AllCategory) { return false; }
            if (transaction == null) { return false; }
            if (transaction.Categories.Contains(targetCategory)) { return false; }


            if (targetCategory == Category.NoCategory)
            {
                transaction.Categories.Clear();
            }
            else
            {
                // Remove from current category if set
                transaction.Categories.Remove(currentCategory);

                // Add category to transaction
                transaction.Categories.Add(targetCategory);
            }

            // Update transaction in repo
            _transactionRepository.Set(transaction);

            return true;
        }
    }
}