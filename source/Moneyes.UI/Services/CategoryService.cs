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
        private readonly ITransactionService _transactionService;
        private readonly IUniqueCachedRepository<TransactionDbo> _transactionRepo;

        private readonly IUniqueCachedRepository<CategoryDbo> _categoryRepo;
        private readonly ICategoryFactory _categoryFactory;

        public CategoryService(ITransactionService transactionService,
            IUniqueCachedRepository<CategoryDbo> categoryRepo,
            ICategoryFactory categoryFactory)
        {
            _transactionService = transactionService;
            _categoryRepo = categoryRepo;
            _categoryFactory = categoryFactory;
        }

        public Category GetCategoryByName(string name)
        {
            var dbo = _categoryRepo.GetAll().FirstOrDefault(c => c.Name.Equals(name));

            return dbo is null
                ? null
                : _categoryFactory.CreateFromDbo(dbo);
        }

        public IEnumerable<Category> GetCategories(CategoryTypes includeCategories = CategoryTypes.All)
        {
            List<Category> categories = new();

            if (includeCategories.HasFlag(CategoryTypes.Real))
            {
                categories = _categoryRepo.GetAll()
                    .Select(c => _categoryFactory.CreateFromDbo(c))
                    .ToList();
            }

            if (!includeCategories.HasFlag(CategoryTypes.AllCategory))
            {
                int allCategoryIndex = categories.IndexOfFirst(c => c.Id.Equals(Category.AllCategoryId));

                if (allCategoryIndex != -1)
                {
                    categories.RemoveAt(allCategoryIndex);
                }
            }
            else if (!categories.Any(c => c.IsAllCategory()))
            {
                categories.Add(Category.AllCategory);
            }


            if (!includeCategories.HasFlag(CategoryTypes.NoCategory))
            {
                int noCategoryIndex = categories.IndexOfFirst(c => c.Id.Equals(Category.NoCategoryId));

                if (noCategoryIndex != -1)
                {
                    categories.RemoveAt(noCategoryIndex);
                }
            }
            else if (!categories.Any(c => c.IsNoCategory()))
            {
                categories.Add(Category.NoCategory);
            }

            //if (!includeCategories.HasFlag(CategoryTypes.Real))
            //{
            //    categories = new Category[] {
            //            _categoryFactory.CreateFromDbo(_categoryRepo.FindById(Category.NoCategory.Id)) ?? Category.NoCategory,
            //            _categoryFactory.CreateFromDbo(_categoryRepo.FindById(Category.AllCategory.Id)) ?? Category.AllCategory
            //        }.Where(c => c != null).ToList();
            //}

            //if (!includeCategories.HasFlag(CategoryTypes.NoCategory))
            //{
            //    categories = categories.Where(c => c.IsNoCategory()).ToList();
            //}

            //if (!includeCategories.HasFlag(CategoryTypes.AllCategory))
            //{
            //    categories = categories.Where(c => c.IsAllCategory()).ToList();
            //}

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

                if (!transaction.Categories.Any(c => category.Id.Equals(c.Id)) &&
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
            try
            {
                var dbo = category.ToDbo(
                    createdAt: DateTime.Now,
                    updatedAt: DateTime.Now);

                return _categoryRepo.Create(dbo) != null;
            }
            catch (CachedRepository<CategoryDbo>.ConstraintViolationException ex)
            {
                if (ex.PropertyName.Equals(nameof(CategoryDbo.Name)))
                {
                    // Name already exists
                }
            }
            catch (DuplicateKeyException)
            {
                // Primary key already exists
            }

            return false;
        }

        public bool UpdateCategory(Category category)
        {
            ArgumentNullException.ThrowIfNull(category, nameof(category));

            if (category.IsNoCategory() || category.IsAllCategory()
                && !_categoryRepo.Contains(category.Id))
            {
                return AddCategory(category);
            }

            return _categoryRepo.Update(category.Id, (existing) =>
            {
                return category.ToDbo(
                    createdAt: existing.CreatedAt,
                    updatedAt: DateTime.Now,
                    isDeleted: existing.IsDeleted);
            });
        }

        public bool DeleteCategory(Category category, bool deleteSubCategories = true)
        {
            ArgumentNullException.ThrowIfNull(category, nameof(category));

            if (_categoryRepo.DeleteById(category.Id))
            {
                if (!deleteSubCategories)
                {
                    return true;
                }

                // Delete sub categories

                var directSubCategories = GetCategories()
                    .Where(c => c.Parent?.Id.Equals(category.Id) ?? false);

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
            return MoveToCategoryInternal(transaction, null, category);
        }

        public bool MoveToCategory(
            Transaction transaction,
            Category currentCategory,
            Category targetCategory)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            if (currentCategory is null)
            {
                throw new ArgumentNullException(nameof(currentCategory));
            }

            if (targetCategory is null)
            {
                throw new ArgumentNullException(nameof(targetCategory));
            }

            if (!transaction.Categories.Any(c => c.Id.Equals(currentCategory.Id)))
            {
                throw new ArgumentException
                    (
                        "The transaction doesn't contain the current category",
                        nameof(currentCategory)
                    );
            }

            return MoveToCategoryInternal(transaction, currentCategory, targetCategory);
        }

        public bool RemoveFromCategory(Transaction transaction, Category category)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            if (category is null)
            {
                throw new ArgumentNullException(nameof(category));
            }

            if (!transaction.Categories.Any(c => c.Id.Equals(category.Id)))
            {
                throw new ArgumentException
                    (
                        "The transaction doesn't contain the given category",
                        nameof(category)
                    );
            }

            if (category.IsAllCategory() || category.IsNoCategory())
            {
                return false;
            }

            return MoveToCategoryInternal(transaction, category, null);
        }

        private bool MoveToCategoryInternal(Transaction transaction, Category? currentCategory, Category? targetCategory)
        {
            if (targetCategory.IsAllCategory()) { return false; }
            if (transaction == null) { return false; }
            if (transaction.Categories.Any(c => c.Id.Equals(targetCategory.Id))) { return false; }

            if (targetCategory.IsNoCategory())
            {
                transaction.Categories.Clear();
            }
            else
            {
                if (currentCategory != null)
                {
                    int removeIndex = transaction.Categories.IndexOfFirst(c => c.Id.Equals(currentCategory.Id));

                    // Remove from current category if set
                    transaction.Categories.RemoveAt(removeIndex);
                }

                if (targetCategory != null)
                {
                    // Add category to transaction
                    transaction.Categories.Add(targetCategory);
                }
            }

            // Update transaction in repo
            return _transactionRepo.Update(transaction.Id, (oldValue) =>
            {
                return transaction.ToDbo(
                     createdAt: oldValue.CreatedAt,
                     updatedAt: DateTime.Now,
                     isDeleted: oldValue.IsDeleted);
            });
        }



        public IEnumerable<Category> GetSubCategories(Category category, int depth = -1)
        {
            if (category.IsNoCategory() || category.IsAllCategory())
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
                if (!category.Parent?.Id.Equals(current.Id) ?? true)
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