using System.Collections.Generic;
using System.Linq;
using LiteDB;
using Moneyes.Core;
using Moneyes.Core.Filters;

namespace Moneyes.Data
{
    public class TransactionRepository : BaseRepository<Transaction>
    {
        public TransactionRepository(ILiteDatabase db) : base(db)
        {
            Collection = Collection.Include(t => t.Categories);
        }

        public IEnumerable<Transaction> GetByCategory(Category category)
        {
            if (category == null || category == Category.AllCategory)
            {
                return AllOrderedByDate();
            }

            if (category == Category.NoCategory)
            {
                return Collection.Query()
                    .Where(t => t.Categories == null || t.Categories.Count == 0)
                    .OrderByDescending(t => t.BookingDate)
                    .ToEnumerable();
            }

            return Collection.Query()
                .Where(t => t.Categories != null && t.Categories.Count > 0)
                .OrderByDescending(t => t.BookingDate)
                .ToEnumerable()
                .Where(t => t.Categories.Any(c => c.Id == category.Id));
        }

        private IEnumerable<Transaction> GetByTransactionType(TransactionType transactionType)
        {
            return Collection.Find(t => t.Type == transactionType);
        }


        public IEnumerable<Transaction> AllOrderedByDate()
        {
            return Collection.Query()
                .OrderByDescending(t => t.BookingDate)
                .ToEnumerable();
        }
        public IEnumerable<Transaction> All(TransactionFilter filter)
        {
            return AllOrderedByDate().Where(t => filter.Evaluate(t));
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

            return Collection.Query()
                .Where(t => t.Categories != null && t.Categories.Count > 0)
                .OrderByDescending(t => t.BookingDate)
                .ToEnumerable()
                .Where(t => t.Categories.Any(category =>
                    notNullCategories.Any(c => c.Id == category.Id)) ||
                    hasNoCategory);
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

        public int InsertAll(IEnumerable<Transaction> transactions)
        {
            int counter = 0;

            foreach (var t in transactions)
            {
                if (Collection.FindById(t.UID) != null) { continue; }

                Collection.Insert(t);
                counter++;
            }

            return counter;
        }
    }
}
