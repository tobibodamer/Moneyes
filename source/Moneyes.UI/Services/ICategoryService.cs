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
        void AssignCategory(Category category);
        Result<Category> GetCategoryByName(string name);

        Result<IEnumerable<Category>> GetCategories(
            CategoryFlags includeCategories = CategoryFlags.All);

        bool AddCategory(Category category);
        bool UpdateCategory(Category category);
        bool DeleteCategory(Category category);

        event Action<Category> CategoryChanged;
    }
}
