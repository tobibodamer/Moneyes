using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Moneyes.UI
{
    public class TransactionService
    {
        private readonly IUniqueCachedRepository<Transaction> _transactionRepository;

        //public event Action<Transaction> EntityAdded;
        //public event Action<Transaction> EntityUpdated;
        //public event Action<Transaction> EntityDeleted;
        public event Action<RepositoryChangedEventArgs<Transaction>> RepositoryChanged;
        public TransactionService(IUniqueCachedRepository<Transaction> transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public IEnumerable<Transaction> GetByCategory(Category category)
        {
            var stopwatch = Stopwatch.StartNew();

            // Category is null or all -> get all transactions
            if (category == null || category.Idquals(Category.AllCategory))
            {
                return AllOrderedByDate();
            }

            // Category is NoCategory -> get all transactions without category
            if (category.Idquals(Category.NoCategory))
            {
                return _transactionRepository
                    .GetAll()
                    .Where(t => t.Categories.Count == 0)
                    .OrderByDescending(t => t.BookingDate)
                    .ToList();
            }

            // Category is some real category
            var t = _transactionRepository.GetAll()
                .Where(t => t.Categories.Any(c => c.Id == category.Id))
                .OrderByDescending(t => t.BookingDate)
                .ToList();

            stopwatch.Stop();
            Debug.WriteLine($"Took {stopwatch.ElapsedMilliseconds}ms to fetch transactions for category {category.Name}.");

            return t;
        }

        private IEnumerable<Transaction> GetByTransactionType(TransactionType transactionType)
        {
            throw new NotImplementedException();
            //return Cache.Values.Where(t => t.Type == transactionType);
        }


        public IEnumerable<Transaction> AllOrderedByDate()
        {
            var stopwatch = Stopwatch.StartNew();

            var transactions = _transactionRepository.GetAll()
                .OrderByDescending(t => t.BookingDate)
                .ThenByDescending(t => t.PartnerIBAN)
                .ThenByDescending(t => t.Index)
                .ToList();

            stopwatch.Stop();
            Debug.WriteLine($"Took {stopwatch.ElapsedMilliseconds}ms to fetch all transactions ordered.");

            return transactions;
        }
        public IEnumerable<Transaction> All(TransactionFilter filter)
        {
            return AllOrderedByDate()
                .Where(t => filter.Evaluate(t));
        }

        public IEnumerable<Transaction> All(params Category[] categories)
        {
            bool hasNoCategory = false;

            if (categories == null)
            {
                return AllOrderedByDate();
            }

            if (categories.Length == 1)
            {
                return GetByCategory(categories[0]);
            }

            // Remove null values, set to null if empty
            var notNullCategories = categories.Where(c => c != null);

            if (!notNullCategories.Any())
            {
                notNullCategories = null;
            }
            else if (notNullCategories.Any(c => c == Category.NoCategory))
            {
                hasNoCategory = true;
            }

            if (notNullCategories == null || notNullCategories.Contains(Category.AllCategory))
            {
                return AllOrderedByDate();
            }

            // Sort into category

            //return Collection.Query()
            //    .Where(t => t.Categories != null && t.Categories.Count > 0)
            //    .OrderByDescending(t => t.BookingDate)
            //    .ToEnumerable()
            //    .Where(t => t.Categories.Any(category =>
            //        notNullCategories.Any(c => c.Id == category.Id)) ||
            //        hasNoCategory);


            var query = _transactionRepository.GetAll();

            var categoryIds = notNullCategories.Select(c => c.Id).ToList();

            if (!hasNoCategory)
            {
                query = query
                    .Where(t => t.Categories.Any(c => categoryIds.Contains(c.Id)));
            }

            return query
                .OrderByDescending(t => t.BookingDate)
                .ToList();
        }

        public IEnumerable<Transaction> All(TransactionFilter filter, params Category[] categories)
        {
            var transactions = All(categories);

            if (filter == null)
            {
                // No need to filter
                return transactions;
            }

            // Apply filter
            return transactions.Where(t => filter.Evaluate(t));
        }

        public DateTime EarliestTransactionDate(TransactionFilter filter)
        {
            var allTransactions = All(filter);

            if (allTransactions.Any())
            {
                return allTransactions.Min(t => t.BookingDate);
            }

            return DateTime.MinValue;
        }

        public DateTime LatestTransactionDate(TransactionFilter filter)
        {
            var allTransactions = All(filter);

            if (allTransactions.Any())
            {
                return allTransactions.Max(t => t.BookingDate);
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// Returns a list of all transactions in the database, including their categories.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<Transaction> GetAllTransactions()
        {
            var stopwatch = Stopwatch.StartNew();

            var t = _transactionRepository.GetAll().ToList();

            stopwatch.Stop();
            Debug.WriteLine($"Took {stopwatch.ElapsedMilliseconds}ms to fetch transactions.");

            return t;
        }

        /// <summary>
        /// Gets a list of all existing transactions found for a given list of <paramref name="transactions"/>.
        /// </summary>
        /// <param name="transactions">The transactions to search for by id.</param>
        /// <param name="onlyDiffering">Return only transactions differing from the existing transaction.</param>
        /// <returns></returns>
        public IReadOnlyList<(Transaction existing, Transaction newTransaction)> GetExistingTransactions(
        IEnumerable<Transaction> transactions, bool onlyDiffering = true)
        {
            var transactionsMap = transactions.ToDictionary(t => t.UID, t => t);
            var transactionUIDs = transactionsMap.Keys.ToList();

            var existingTransactions = _transactionRepository.GetAll()
                .Where(t => transactionUIDs.Contains(t.UID))
                .ToList();

            return existingTransactions
                .Select(e => (e, transactionsMap[e.UID]))
                .Where(x => !onlyDiffering || !TransactionEquals(x.e, x.Item2))
                .ToList();
        }

        public Transaction? GetByUID(string uid)
        {
            return _transactionRepository.GetAll().FirstOrDefault(t => t.UID.Equals(uid));
        }

        /// <summary>
        /// Add multiple transactions to the database.
        /// </summary>
        /// <param name="transactions"></param>
        /// <returns></returns>
        public bool AddTransactions(IEnumerable<Transaction> transactions)
        {
            //TODO: implemetn insert all
            //_transactionRepository.CreateAll

            //OnRepositoryChanged(RepositoryChangedAction.Add,
            //    addedItems: transactions.ToList());

            throw new NotImplementedException();
        }

        /// <summary>
        /// Imports the <paramref name="transaction"/> into the database, 
        /// by either inserting it if not already existing,
        /// or updating the already existing transaction.
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns><see langword="true"/> if the transaction was inserted, <see langword="false"/> otherwise.</returns>
        //TODO!!!

        public bool ImportTransaction(Transaction transaction)
        {
            //var existing = await GetByUID(transaction.UID);

            //_transactionRepository.

            //if (existing != null)
            //{
            //    _transactionRepository.Update(transaction)
            //}

            throw new NotImplementedException();
        }

        /// <summary>
        /// Imports the <paramref name="transactions"/> into the database, 
        /// by either inserting them or updating the already existing transaction.
        /// </summary>
        /// <param name="transactions"></param>
        /// <returns>The number of transactions inserted.</returns>
        public int ImportTransactions(IEnumerable<Transaction> transactions)
        {
            //// Get existing transactions and track including categories

            //var transactionUIDs = transactions.Select(t => t.UID).ToList();
            //var existing = await ctx.Transactions
            //    .Include(t => t.Categories)
            //    .Where(t => transactionUIDs.Contains(t.UID))
            //    .ToDictionaryAsync(t => t.UID, t => t);


            //// Update values of existing transaction

            //var transactionsToUpdate = transactions
            //    .Where(t => existing.ContainsKey(t.UID))
            //    .ToList();

            //await ctx.Transactions.UpsertRange(transactionsToUpdate)
            //    .AllowIdentityMatch()
            //    .RunAsync();


            //// Update category assignments of existing transactions

            //foreach (var transaction in transactions)
            //{
            //    var existingTransaction = existing[transaction.UID];
            //    var categories = transaction.Categories.ToList();

            //    existingTransaction.Categories.Clear();
            //    existingTransaction.Categories.AddRange(categories);
            //}


            //await ctx.SaveChangesAsync();

            //// Add new transcations

            //var transactionsToAdd = transactions
            //    .Where(t => !existing.ContainsKey(t.UID))
            //    .ToList();

            //await AddTransactions(transactionsToAdd);

            //OnRepositoryChanged(RepositoryChangedAction.Add | RepositoryChangedAction.Replace,
            //        addedItems: transactionsToAdd,
            //        replacedItems: transactionsToUpdate);

            //return transactionsToAdd.Count;

            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the given transactions in the database.
        /// </summary>
        /// <param name="transactions"></param>
        /// <returns></returns>
        //public void UpdateTransactions(IEnumerable<Transaction> transactions)
        //{
        //    var uids = transactions.Select(t => t.UID).ToList();
        //    var existing = _transactionRepository.GetAll()
        //        .Where(t => uids.Contains(t.UID))
        //        .ToList();

        //    var existingUIDs = existing.Select(t => t.UID).ToList();

            
        //    foreach (var transaction in GetExistingTransactions(transactions))
            
        //    transactions.Where(t => existingUIDs.Contains(t.UID))
        //}

        /// <summary>
        /// Updates the categories of the <paramref name="transaction"/> in the database.
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        //public async Task UpdateOnlyCategories(Transaction transaction)
        //{
        //    using var ctx = _dbContextFactory.CreateDbContext();

        //    ctx.AttachCategoriesOf(transaction);

        //    var existingTransaction = ctx.Transactions
        //        .Include(t => t.Categories)
        //        .FirstOrDefault(t => transaction.UID.Equals(t.UID));

        //    var categories = transaction.Categories.ToList();

        //    existingTransaction.Categories.Clear();
        //    existingTransaction.Categories.AddRange(categories);

        //    await ctx.SaveChangesAsync();
        //}

        /// <summary>
        /// Updates the categories of the <paramref name="transactions"/> in the database.
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        //public async Task UpdateOnlyCategories(IEnumerable<Transaction> transactions)
        //{
        //    using var ctx = _dbContextFactory.CreateDbContext();

        //    ctx.AttachAndReplaceCategoriesOf(transactions);

        //    var uids = transactions.Select(t => t.UID).ToList();
        //    var existing = await ctx.Transactions
        //        .Include(t => t.Categories)
        //        .Where(t => uids.Contains(t.UID))
        //        .ToDictionaryAsync(t => t.UID, t => t);

        //    foreach (var transaction in transactions)
        //    {
        //        var existingTransaction = existing[transaction.UID];
        //        var categories = transaction.Categories.ToList();

        //        existingTransaction.Categories.Clear();
        //        existingTransaction.Categories.AddRange(categories);
        //    }

        //    await ctx.SaveChangesAsync();
        //}

        /// <summary>
        /// Determines whether two <see cref="Transaction"/>s are equal.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        private static bool TransactionEquals(Transaction left, Transaction other)
        {
            return other is Transaction transaction &&
                   left.ValueDate == transaction.ValueDate &&
                   left.Purpose == transaction.Purpose &&
                   left.BookingType == transaction.BookingType &&
                   left.IBAN == transaction.IBAN &&
                   left.PartnerIBAN == transaction.PartnerIBAN &&
                   left.BIC == transaction.BIC &&
                   left.Name == transaction.Name &&
                   left.Currency == transaction.Currency;
        }
    }
}