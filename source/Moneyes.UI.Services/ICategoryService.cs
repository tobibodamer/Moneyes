using Moneyes.Core;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;

namespace Moneyes.UI
{
    public interface ICategoryService
    {
        Category? GetCategoryByName(string name);

        IEnumerable<Category> GetCategories(CategoryTypes includeCategories = CategoryTypes.All);

        bool AddCategory(Category category);
        bool UpdateCategory(Category category);
        bool DeleteCategory(Category category, bool deleteSubCategories = true);

        /// <summary>
        /// Get alls sub categories of a category up to a certain depth.
        /// </summary>
        /// <param name="category">The trunk category.</param>
        /// <param name="depth">The max depth of sub categories to search <br></br>
        /// (0 - no sub categories, -1 - all) </param>
        /// <returns></returns>
        IEnumerable<Category> GetSubCategories(Category category, int depth = -1);
        IEnumerable<CategoryWithChildren> GetCategoriesWithChildren(CategoryTypes includeCategories = CategoryTypes.All);
    }
}
