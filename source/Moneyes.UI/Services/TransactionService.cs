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
    public class TransactionService : ITransactionService
    {
        private readonly IUniqueCachedRepository<TransactionDbo> _transactionRepository;
        private readonly ITransactionFactory _transactionFactory;
        public TransactionService(IUniqueCachedRepository<TransactionDbo> transactionRepository,
            ITransactionFactory transactionFactory)
        {
            _transactionRepository = transactionRepository;
            _transactionFactory = transactionFactory;
        }

        public IEnumerable<Transaction> GetByCategory(Category category)
        {
            var stopwatch = Stopwatch.StartNew();

            // Category is null or all -> get all transactions
            if (category == null || category.Id.Equals(Category.AllCategoryId))
            {
                return AllOrderedByDate();
            }

            // Category is NoCategory -> get all transactions without category
            if (category.IsNoCategory())
            {
                return _transactionRepository
                    .GetAll()
                    .AsParallel()
                    .Where(t => t.Category == null)
                    .OrderByDescending(t => t.BookingDate)
                    .Select(_transactionFactory.CreateFromDbo)
                    .ToList();
            }

            // Category is some real category
            var t = _transactionRepository.GetAll()
                .AsParallel()
                .Where(t => t.Category?.Id == category.Id)
                .OrderByDescending(t => t.BookingDate)
                .Select(_transactionFactory.CreateFromDbo)
                .ToList();

            stopwatch.Stop();
            Debug.WriteLine($"Took {stopwatch.ElapsedMilliseconds}ms to fetch transactions for category {category.Name}.");

            return t;
        }
        public IEnumerable<Transaction> AllOrderedByDate()
        {
            var stopwatch = Stopwatch.StartNew();

            var transactions = _transactionRepository.GetAll()
                .AsParallel()
                .OrderByDescending(t => t.BookingDate)
                .ThenByDescending(t => t.PartnerIBAN)
                .ThenByDescending(t => t.Index)
                .Select(_transactionFactory.CreateFromDbo);

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
                    .Where(t => t.Category != null && categoryIds.Contains(t.Category.Id));
            }

            return query
                .OrderByDescending(t => t.BookingDate)
                .Select(_transactionFactory.CreateFromDbo)
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

            var t = _transactionRepository.GetAll()
                .Select(_transactionFactory.CreateFromDbo)
                .ToList();

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
        //public IReadOnlyList<(Transaction existing, Transaction newTransaction)> GetExistingTransactions(
        //IEnumerable<Transaction> transactions, bool onlyDiffering = true)
        //{
        //    var transactionsMap = transactions.ToDictionary(t => t.UID, t => t);
        //    var transactionUIDs = transactionsMap.Keys.ToList();

        //    var existingTransactions = _transactionRepository.GetAll()
        //        .Where(t => transactionUIDs.Contains(t.UID))
        //        .ToList();

        //    return existingTransactions
        //        .Select(e => (e, transactionsMap[e.UID]))
        //        .Where(x => !onlyDiffering || !TransactionEquals(x.e, x.Item2))
        //        .ToList();
        //}

        public Transaction? GetByUID(string uid)
        {
            var transactionDbo = _transactionRepository.GetAll()
                .FirstOrDefault(t => t.UID.Equals(uid));

            if (transactionDbo is null)
            {
                return null;
            }

            return _transactionFactory.CreateFromDbo(transactionDbo);
        }

        public bool ImportTransaction(Transaction transaction)
        {
            var transactionDbo = transaction.ToDbo();

            return _transactionRepository.Set(transactionDbo, onConflict: UniqueConflictResolutionAction.UpdateContentOrIgnore);
        }

        /// <summary>
        /// Imports the <paramref name="transactions"/> into the database, 
        /// by either inserting them or updating the already existing transaction.
        /// </summary>
        /// <param name="transactions"></param>
        /// <returns>The number of transactions inserted.</returns>
        public int ImportTransactions(IEnumerable<Transaction> transactions)
        {
            // TODO: Validate if duplicate categories and all categories exist, maybe in repo?

            return _transactionRepository.SetMany(transactions.Select(x => x.ToDbo()),
                onConflict: UniqueConflictResolutionAction.UpdateContentOrIgnore);
        }
    }
}