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
        private readonly TransactionService _transactionService;
        private readonly IUniqueCachedRepository<Category> _categoryRepo;

        public CategoryService(TransactionService transactionService,
            IUniqueCachedRepository<Category> categoryRepo)
        {
            _transactionService = transactionService;
            _categoryRepo = categoryRepo;
        }

        public Category GetCategoryByName(string name)
        {
            return _categoryRepo.GetAll().FirstOrDefault(c => c.Name.Equals(name));
        }

        public IEnumerable<Category> GetCategories(CategoryTypes includeCategories = CategoryTypes.All)
        {
            IList<Category> categories =
                _categoryRepo.GetAll().ToList();

            if (includeCategories.HasFlag(CategoryTypes.AllCategory)
                && !categories.Any(c => c.Idquals(Category.AllCategory)))
            {
                categories.Add(Category.AllCategory);
            }

            if (includeCategories.HasFlag(CategoryTypes.NoCategory)
                && !categories.Any(c => c.Idquals(Category.NoCategory)))
            {
                categories.Add(Category.NoCategory);
            }

            if (!includeCategories.HasFlag(CategoryTypes.Real))
            {
                categories = new Category[] {
                        _categoryRepo.FindById(Category.NoCategory.Id) ?? Category.NoCategory,
                        _categoryRepo.FindById(Category.AllCategory.Id)  ?? Category.AllCategory
                    }.Where(c => c != null).ToList();
            }

            if (!includeCategories.HasFlag(CategoryTypes.NoCategory))
            {
                categories = categories.Where(c => c != Category.NoCategory).ToList();
            }

            if (!includeCategories.HasFlag(CategoryTypes.AllCategory))
            {
                categories = categories.Where(c => c != Category.AllCategory).ToList();
            }

            return categories;
        }

        public void AssignCategories(Transaction transaction,
            AssignMethod assignMethod = AssignMethod.KeepPrevious,
            bool updateDatabase = false)
        {
            // Get old transaction
            Transaction oldTransaction = null;

            if (assignMethod is not AssignMethod.Simple or AssignMethod.Reset)
            {
                oldTransaction = _transactionService.GetByUID(transaction.UID);
            }

            // Assign categories

            List<Category> categories = GetCategories(CategoryTypes.Real)
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
                        _transactionService.ImportTransaction(transaction);
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
                _transactionService.ImportTransaction(transaction);
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
                oldTransactions = _transactionService.All()
                    .ToDictionary(t => t.UID, t => t);
            }

            // Assign categories

            List<Category> categories = GetCategories(CategoryTypes.Real)
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
            }

            if (updateDatabase)
            {
                // Store
                _transactionService.ImportTransactions(transactionsToUpdate);
            }
        }

        public void ReassignCategories(AssignMethod assignMethod = AssignMethod.Simple)
        {
            if (assignMethod is AssignMethod.KeepPrevious) { return; }

            IEnumerable<Transaction> transactions = _transactionService.All();

            AssignCategories(transactions, assignMethod, true);
        }

        public int AssignCategory(Category category, AssignMethod assignMethod = AssignMethod.KeepPrevious)
        {
            // Get transactions
            var transactions = _transactionService.All().ToList();
            var transactionsToUpdate = new List<Transaction>();

            if (category.Filter == null) { return 0; }

            foreach (Transaction transaction in transactions)
            {
                if (assignMethod is AssignMethod.Reset)
                {
                    transaction.Categories.Clear();
                }

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
            _ = _transactionService.ImportTransactions(transactionsToUpdate);

            return transactionsToUpdate.Count;
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
            if (_categoryRepo.Delete(category))
            {
                var transactions = _transactionService.GetByCategory(category).ToList();

                foreach (var transaction in transactions)
                {
                    transaction.Categories.RemoveAll(c => c.Idquals(category));
                }

                //TODO: 
                //_transactionService.UpdateTransactions(transactions);

                var directSubCategories = GetCategories()
                    .Where(c => c.Parent?.Idquals(category) ?? false);

                foreach (var subCategory in directSubCategories)
                {
                    DeleteCategory(subCategory);
                }

                return true;
            }

            return false;
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
            _transactionService.ImportTransaction(transaction);

            return true;
        }

        public bool RemoveFromCategory(Transaction transaction, Category category)
        {
            if (transaction == null ||
                category == null ||
                !transaction.Categories.Contains(category) ||
                category == Category.AllCategory ||
                category == Category.NoCategory)
            {
                return false;
            }

            if (transaction.Categories.Remove(category))
            {
                _transactionService.ImportTransaction(transaction);

                return true;
            }

            return false;
        }

        public IEnumerable<Category> GetSubCategories(Category category, int depth = -1)
        {
            if (category == Category.NoCategory || category == Category.AllCategory)
            {
                return Enumerable.Empty<Category>();
            }

            IEnumerable<Category> categories = GetCategories(CategoryTypes.Real)
                .ToList();

            return GetSubCategoriesRecursive(category, categories, depth, 0).Distinct();
        }

        private IEnumerable<Category> GetSubCategoriesRecursive(
            Category current, IEnumerable<Category> allCategories,
            int maxDepth, int currentDepth)
        {
            foreach (Category category in allCategories)
            {
                if (!category.Parent?.Idquals(current) ?? true)
                {
                    continue;
                }

                yield return category;

                if (maxDepth > 0 && currentDepth >= maxDepth)
                {
                    continue;
                }

                IEnumerable<Category> subCategories = GetSubCategoriesRecursive(
                    category, allCategories, maxDepth, ++currentDepth);

                foreach (Category subCategory in subCategories)
                {
                    yield return subCategory;
                }
            }
        }
    }
}