using Moneyes.Core;
using Moneyes.LiveData;
using System.Collections.Generic;

namespace Moneyes.UI
{
    public interface ICategoryService
    {
        //TODO: Move to transaction service??
        void SortIntoCategories(
            IEnumerable<Transaction> transactions,
            AssignMethod assignMethod = AssignMethod.KeepPrevious,
            bool updateDatabase = false);

        Result<Category> GetCategoryByName(string name);

        Result<IEnumerable<Category>> GetCategories();
    }
}
