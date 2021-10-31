using Moneyes.Core;
using Moneyes.LiveData;
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

        Result<Category> GetCategoryByName(string name);

        Result<IEnumerable<Category>> GetCategories(
            CategoryFlags includeCategories = CategoryFlags.All);

        bool AddCategory(Category category);
    }
}
