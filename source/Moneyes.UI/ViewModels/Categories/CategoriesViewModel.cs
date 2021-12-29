using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using Moneyes.UI.Services;

namespace Moneyes.UI.ViewModels
{
    internal class CategoriesViewModel : CategoriesViewModelBase<CategoryViewModel>
    {
        public CategoriesViewModel(CategoryViewModelFactory factory,
            ICategoryService categoryService, IStatusMessageService statusMessageService)
            : base(factory, categoryService, statusMessageService)
        {
        }
        protected override CategoryViewModel CreateEntry(Category category, TransactionFilter filter, CategoryTypes categoryTypes, bool flat)
        {
            return Factory.CreateCategoryViewModel(category,
                getCurrentCategory: () => SelectedCategory?.Category,
                editViewModel =>
                {
                    EditCategoryViewModel = editViewModel;
                });
        }
    }
}
