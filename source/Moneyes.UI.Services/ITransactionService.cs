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
        Transaction? GetByUID(string uid);
        bool ImportTransaction(Transaction transaction);
        int ImportTransactions(IEnumerable<Transaction> transactions);
        DateTime LatestTransactionDate(TransactionFilter filter);


        /// <summary>
        /// Assigns the given <paramref name="transaction"/> to the first category
        /// with a <see cref="Category.Filter"/> matching the transaction, by setting the <see cref="Transaction.Category"/>.
        /// </summary>
        /// <param name="transaction">The transaction to assign.</param>
        /// <param name="assignMethod">The method to use when assigning the transaction./param>
        void AssignCategory(Transaction transaction, AssignMethod assignMethod = AssignMethod.KeepPreviousAlways);

        /// <summary>
        /// Assigns the given <paramref name="transactions"/> to the first category
        /// with a <see cref="Category.Filter"/> matching the transaction, by setting the <see cref="Transaction.Category"/>.
        /// </summary>
        /// <param name="transactions">The transactions to assign.</param>
        /// <param name="assignMethod">The method to use when assigning the transactions./param>
        void AssignCategories(IEnumerable<Transaction> transactions, AssignMethod assignMethod = AssignMethod.KeepPreviousAlways);

        /// <summary>
        /// Updates the category assignment for the given <paramref name="category"/> of all transactions, 
        /// and updates the database.
        /// </summary>
        /// <param name="category">The category to reassign.</param>
        /// <param name="assignMethod">The method to use when assigning the transactions./param>
        int ReassignCategory(Category category, AssignMethod assignMethod = AssignMethod.Simple);

        /// <summary>
        /// Updates the category assignment of all transactions, and updates the database.
        /// </summary>
        /// <param name="assignMethod">The method to use when assigning the transactions./param>
        int ReassignCategories(AssignMethod assignMethod = AssignMethod.Simple);

        /// <summary>
        /// Moves the <paramref name="transaction"/> to the <paramref name="category"/> 
        /// by setting the <see cref="Transaction.Category"/> and updating the database.
        /// </summary>
        /// <param name="transaction">The transaction to move.</param>
        /// <param name="category">The real target category.</param>
        /// <returns><see langword="true"/> if the transaction was successfully moved.</returns>
        bool MoveToCategory(Transaction transaction, Category category);

        /// <summary>
        /// Removes the <paramref name="transaction"/> from its category
        /// by setting the <see cref="Transaction.Category"/> to <see langword="null"/> and updating the database.
        /// </summary>
        /// <param name="transaction">The transaction to remove.</param>
        /// <returns><see langword="true"/> if the transaction changed.</returns>
        bool RemoveFromCategory(Transaction transaction);
    }
}