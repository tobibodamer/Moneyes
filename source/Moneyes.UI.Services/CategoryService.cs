using Moneyes.Core;
using Moneyes.Data;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Moneyes.UI
{
    public class CategoryWithChildren : Category
    {
        public CategoryWithChildren(Category category) : base(category.Id)
        {
            this.Parent = category.Parent;
            this.Name = category.Name;
            this.Filter = category.Filter;
            this.Target = category.Target;
        }

        public HashSet<CategoryWithChildren> SubCategories { get; init; } = new();
    }
    public class CategoryService : ICategoryService
    {

        private readonly IUniqueCachedRepository<CategoryDbo> _categoryRepo;
        private readonly ICategoryFactory _categoryFactory;

        public CategoryService(
            IUniqueCachedRepository<CategoryDbo> categoryRepo,
            ICategoryFactory categoryFactory)
        {
            _categoryRepo = categoryRepo;
            _categoryFactory = categoryFactory;
        }

        public Category? GetCategoryByName(string name)
        {
            var dbo = _categoryRepo.FirstOrDefault(c => c.Name.Equals(name));

            return dbo is null
                ? null
                : _categoryFactory.CreateFromDbo(dbo);
        }

        public IEnumerable<CategoryWithChildren> GetCategoriesWithChildren(CategoryTypes includeCategories = CategoryTypes.All)
        {
            var categories = GetCategories(includeCategories).Select(c => new CategoryWithChildren(c)).ToList();

            // Select all categories with a matching parent
            var categoriesWithParent = categories.Where(
                c => c.Parent is not null).ToList();

            // Add categories with parent to sub categories of parent
            foreach (var category in categoriesWithParent)
            {
                var parent = category.Parent;
                var parentCategoryViewModel = categories.FirstOrDefault(c => c.Id == parent!.Id);

                parentCategoryViewModel?.SubCategories.Add(category);
            }

            // Remove all categories with a parent (from top level)
            categories.RemoveAll(c => categoriesWithParent.Contains(c));

            //HashSet<CategoryH> hierachical = new();

            //do {
            //    foreach (var category in categories)
            //    {
            //        if (category.Parent == null)
            //        {
            //            hierachical.Add(new(category.Id));
            //            categories.Remove(category);
            //        }
            //        else
            //        {

            //        }
            //        var parent = category.Parent;
            //        var parentCategoryViewModel = flatCategories.FirstOrDefault(c => c.Category.Id == parent.Id);

            //        parentCategoryViewModel.SubCatgeories.Add(category);
            //    }
            //} while (categories.Count > 0);

            return categories;
        }

        public IEnumerable<Category> GetCategories(CategoryTypes includeCategories = CategoryTypes.All)
        {
            List<Category> categories = new();

            if (includeCategories.HasFlag(CategoryTypes.Real))
            {
                categories = _categoryRepo
                    .Select(c => _categoryFactory.CreateFromDbo(c))
                    .ToList();
            }

            if (!includeCategories.HasFlag(CategoryTypes.AllCategory))
            {
                int allCategoryIndex = IndexOfFirst(categories, c => c.Id.Equals(Category.AllCategoryId));

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
                int noCategoryIndex = IndexOfFirst(categories, c => c.Id.Equals(Category.NoCategoryId));

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



        public bool AddCategory(Category category)
        {
            try
            {
                return _categoryRepo.Create(category.ToDbo()) != null;
            }
            catch (ConstraintViolationException ex)
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

            return _categoryRepo.Set(category.ToDbo());
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

        private static int IndexOfFirst<T>(IList<T> list, Func<T, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (predicate(list[i]))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}