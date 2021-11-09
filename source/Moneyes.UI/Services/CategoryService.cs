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
        private readonly TransactionRepository _transactionRepository;
        private readonly CategoryRepository _categoryRepo;

        public CategoryService(TransactionRepository transactionRepo,
            CategoryRepository categoryRepo)
        {
            _transactionRepository = transactionRepo;
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
                    _categoryRepo.GetAll();

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
                oldTransaction = _transactionRepository.FindById(transaction.UID);
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
                        _transactionRepository.Set(transaction);
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
                _transactionRepository.Set(transaction);
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
                oldTransactions = _transactionRepository.All()
                    .ToDictionary(t => t.UID, t => t);
            }

            // Assign categories

            List<Category> categories = GetCategories(CategoryFlags.Real).Data
                .OrderBy(c => c.IsExlusive)
                .ToList();

            List<Transaction> transactionsToUpdate = new();

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

                transactionsToUpdate.Add(transaction);

                // Store
                _transactionRepository.Set(transactionsToUpdate);
            }
        }

        public void ReassignCategories(AssignMethod assignMethod = AssignMethod.Simple)
        {
            if (assignMethod is AssignMethod.KeepPrevious) { return; }

            IEnumerable<Transaction> transactions = _transactionRepository.All();

            AssignCategories(transactions, assignMethod, true);
        }

        public void AssignCategory(Category category)
        {
            // Get transactions
            var transactions = _transactionRepository.All().ToList();
            var transactionsToUpdate = new List<Transaction>();

            if (category.Filter == null) { return; }

            foreach (Transaction transaction in transactions)
            {
                if (category.IsExlusive && transaction.Categories.Any())
                {
                    // Exclusive category and already assigned
                    continue;
                }

                if (!transaction.Categories.Any(c => category.Idquals(c)) &&
                    category.Filter != null && category.Filter.Evaluate(transaction))
                {
                    transaction.Categories.Add(category);
                    transactionsToUpdate.Add(transaction);
                }
            }

            // Store
            _transactionRepository.Set(transactionsToUpdate);
        }

        public bool AddCategory(Category category)
        {
            return _categoryRepo.Create(category) != null;
        }

        public bool UpdateCategory(Category category)
        {
            return _categoryRepo.Set(category);
        }

        public bool DeleteCategory(Category category)
        {
            return _categoryRepo.Delete(category.Id);
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

        public IEnumerable<Category> GetSubCategories(Category category, int depth = -1)
        {
            if (category == Category.NoCategory || category == Category.AllCategory)
            {
                return Enumerable.Empty<Category>();
            }

            IEnumerable<Category> categories = GetCategories(CategoryFlags.Real)
                .GetOrNull();

            return GetSubCategoriesRecursive(category, categories, depth, 0);
        }

        private IEnumerable<Category> GetSubCategoriesRecursive(
            Category current, IEnumerable<Category> allCategories,
            int maxDepth, int currentDepth)
        {
            foreach (Category category in allCategories)
            {
                if (!category.Parent.Idquals(current))
                {
                    continue;
                }

                yield return category;

                if (maxDepth > 0 && currentDepth >= maxDepth)
                {
                    continue;
                }

                IEnumerable<Category> subCategories = GetSubCategoriesRecursive(
                    category, allCategories, maxDepth, currentDepth++);

                foreach (Category subCategory in subCategories)
                {
                    yield return subCategory;
                }
            }
        }
    }
}