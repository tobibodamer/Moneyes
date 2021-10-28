using Moneyes.Core;
using Moneyes.Data;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Moneyes.UI
{
    public class CategoryService : ICategoryService
    {
        private readonly TransactionRepository _transactionRepo;
        private readonly CategoryRepository _categoryRepo;

        public CategoryService(TransactionRepository transactionRepo, 
            CategoryRepository categoryRepo)
        {
            _transactionRepo = transactionRepo;
            _categoryRepo = categoryRepo;
        }

        public Result<Category> GetCategoryByName(string name)
        {
            try
            {
                return _categoryRepo.FindByName(name);
            }
            catch (Exception)
            {
                return Result.Failed<Category>();
                //TODO: Log
            }
        }

        public Result<IEnumerable<Category>> GetCategories()
        {
            try
            {
                var categories = _categoryRepo.All();


                return Result.Successful(categories);
            }
            catch (Exception)
            {
                return Result.Failed<IEnumerable<Category>>();
                //TODO: Log
            }
        }

        public void SortIntoCategories(IEnumerable<Transaction> transactions,
            AssignMethod assignMethod = AssignMethod.KeepPrevious,
            bool updateDatabase = false)
        {
            // Get old transactions
            Dictionary<string, Transaction> oldTransactions = null;

            if (assignMethod is not AssignMethod.Simple)
            {
                oldTransactions = _transactionRepo.All()
                    .ToDictionary(t => t.UID, t => t);
            }

            // Assign categories

            List<Category> categories = _categoryRepo.All()
                .OrderBy(c => c.IsExlusive)
                .Where(c => c != Category.AllCategory)
                .Where(c => c != Category.NoCategory)
                .ToList();

            foreach (Transaction transaction in transactions)
            {
                if (assignMethod is not AssignMethod.Simple &&
                    oldTransactions.TryGetValue(transaction.UID, out Transaction oldTransaction))
                {
                    // Transaction already imported -> keep old categories
                    transaction.Categories = oldTransaction.Categories;

                    // Dont merge if keep previous method is used
                    if (assignMethod is AssignMethod.KeepPrevious)
                    {
                        continue;
                    }
                }

                // Transaction not imported or merge -> assign new categories
                foreach (Category category in categories)
                {
                    if (category.IsExlusive && transaction.Categories.Any())
                    {
                        // Exclusive category and already assigned
                        continue;
                    }

                    if (category.Filter != null && category.Filter.Evaluate(transaction))
                    {
                        transaction.Categories.Add(category);
                    }
                }
            }
        }
    }
}
