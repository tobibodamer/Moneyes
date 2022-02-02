using Moneyes.Core;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;

namespace Moneyes.UI
{
    public interface ICategoryService
    {
        //TODO: Move to transaction service??
        void AssignCategories(
            IEnumerable<Transaction> transactions,
            AssignMethod assignMethod = AssignMethod.KeepPrevious,
            bool updateDatabase = false);

        void ReassignCategories(AssignMethod assignMethod = AssignMethod.Simple);

        /// <summary>
        /// Assigns a given category to all transactions matching, and updates the database.
        /// </summary>
        /// <param name="category"></param>
        int AssignCategory(Category category, AssignMethod assignMethod = AssignMethod.KeepPrevious);


        bool MoveToCategory(Transaction transaction, Category category);

        bool RemoveFromCategory(Transaction transaction);

        Category GetCategoryByName(string name);

        IEnumerable<Category> GetCategories(
            CategoryTypes includeCategories = CategoryTypes.All);

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
    }
}
