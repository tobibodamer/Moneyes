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

        public event Action<Category> CategoryChanged;
        public event Action<Category> CategoryAdded;
        public event Action<Category> CategoryDeleted;

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

        public Result<IEnumerable<Category>> GetCategories(CategoryFlags includeCategories = CategoryFlags.All)
        {
            try
            {
                IEnumerable<Category> categories =
                    _categoryRepo.All();

                if (!includeCategories.HasFlag(CategoryFlags.Real))
                {
                    categories = new Category[] {
                        _categoryRepo.FindById(Category.NoCategory.Id),
                        _categoryRepo.FindById(Category.AllCategory.Id)
                    }.Where(c => c != null);
                }

                if (!includeCategories.HasFlag(CategoryFlags.NoCategory))
                {
                    categories = categories.Where(c => c != Category.NoCategory);
                }

                if (!includeCategories.HasFlag(CategoryFlags.AllCategory))
                {
                    categories = categories.Where(c => c != Category.AllCategory);
                }

                return Result.Successful(categories);
            }
            catch (Exception)
            {
                return Result.Failed<IEnumerable<Category>>();
                //TODO: Log
            }
        }

        public void AssignCategories(Transaction transaction,
            AssignMethod assignMethod = AssignMethod.KeepPrevious,
            bool updateDatabase = false)
        {
            // Get old transaction
            Transaction oldTransaction = null;

            if (assignMethod is not AssignMethod.Simple or AssignMethod.Reset)
            {
                oldTransaction = _transactionRepo.FindById(transaction.UID);
            }

            // Assign categories

            List<Category> categories = GetCategories(CategoryFlags.Real).Data
                .OrderBy(c => c.IsExlusive)
                .ToList();

            if (assignMethod is AssignMethod.Reset)
            {
                transaction.Categories.Clear();
            }

            if (assignMethod is not AssignMethod.Simple && oldTransaction != null)
            {
                // Transaction already imported -> keep old categories
                transaction.Categories = oldTransaction.Categories;

                // Dont merge if keep previous method is used
                if (assignMethod is AssignMethod.KeepPrevious)
                {
                    if (updateDatabase)
                    {
                        _transactionRepo.Set(transaction);
                    }

                    return;
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

            if (updateDatabase)
            {
                _transactionRepo.Set(transaction);
            }
        }


        public void AssignCategories(IEnumerable<Transaction> transactions,
            AssignMethod assignMethod = AssignMethod.KeepPrevious,
            bool updateDatabase = false)
        {
            // Get old transactions
            Dictionary<string, Transaction> oldTransactions = null;

            if (assignMethod is not AssignMethod.Simple or AssignMethod.Reset)
            {
                oldTransactions = _transactionRepo.All()
                    .ToDictionary(t => t.UID, t => t);
            }

            // Assign categories

            List<Category> categories = GetCategories(CategoryFlags.Real).Data
                .OrderBy(c => c.IsExlusive)
                .ToList();

            foreach (Transaction transaction in transactions)
            {
                if (assignMethod is AssignMethod.Reset)
                {
                    transaction.Categories.Clear();
                }

                if (assignMethod is not AssignMethod.Simple or AssignMethod.Reset &&
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

                // Store
                _transactionRepo.Set(transactions);
            }
        }

        public void ReassignCategories(AssignMethod assignMethod = AssignMethod.Simple)
        {
            if (assignMethod is AssignMethod.KeepPrevious) { return; }

            IEnumerable<Transaction> transactions = _transactionRepo.All();

            AssignCategories(transactions, assignMethod, true);
        }

        public void AssignCategory(Category category)
        {
            // Get transactions
            var transactions = _transactionRepo.All();

            if (category.Filter == null) { return; }

            foreach (Transaction transaction in transactions)
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

            // Store
            _transactionRepo.Set(transactions);
        }

        public bool AddCategory(Category category)
        {
            //TODO: Use real insert and dont update if existing (return false)
            if (_categoryRepo.Set(category))
            {
                OnCategoryAdded(category);
                return true;
            }

            return false;
        }

        public bool UpdateCategory(Category category)
        {
            if (_categoryRepo.Set(category))
            {
                OnCategoryAdded(category);
            }
            else
            {
                OnCategoryChanged(category);
            }

            return true;
        }

        public bool DeleteCategory(Category category)
        {
            OnCategoryDeleted(category);
            throw new NotImplementedException();
        }

        protected virtual void OnCategoryChanged(Category c)
        {
            CategoryChanged?.Invoke(c);
        }

        protected virtual void OnCategoryAdded(Category c)
        {
            CategoryAdded?.Invoke(c);
        }

        protected virtual void OnCategoryDeleted(Category c)
        {
            CategoryDeleted?.Invoke(c);
        }
    }
}