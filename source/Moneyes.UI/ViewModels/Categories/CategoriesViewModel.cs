using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using Moneyes.UI.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moneyes.UI.ViewModels
{
    internal class CategoriesViewModel : CategoriesViewModelBase<CategoryViewModel>
    {
        public CategoriesViewModel(CategoryViewModelFactory factory,
            ICategoryService categoryService, IStatusMessageService statusMessageService)
            : base(factory, categoryService, statusMessageService)
        {
        }

        protected override Task<List<CategoryViewModel>> GetCategoriesAsync(TransactionFilter filter, CategoryTypes categoryTypes, bool flat)
        {
            return Task.Run(() =>
            {
                List<CategoryViewModel> categoryViewModels = new();

                var categories = CategoryService.GetCategories(categoryTypes);

                foreach (var category in categories)
                {
                    categoryViewModels.Add(
                        Factory.CreateCategoryViewModel(category,
                            editViewModel =>
                            {
                                EditCategoryViewModel = editViewModel;
                            }));
                }

                if (!flat)
                {
                    // Set sub categories
                    SetSubCategories(categoryViewModels);
                }

                return categoryViewModels;
            });
        }
    }
}
