using Moneyes.Core;
using Moneyes.LiveData;
using System;
using System.Linq;

namespace Moneyes.UI.ViewModels
{
    class CategoryViewModelFactory
    {
        public CategoryViewModelFactory(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }
        public CategoryViewModel CreateCategoryViewModel(Category category, Action<EditCategoryViewModel> editAction)
        {
            var categoryViewModel = new CategoryViewModel
            {
                Category = category,
                EditCommand = new AsyncCommand(async ct =>
                {
                    EditCategoryViewModel editViewModel = CreateEditCategoryViewModel(category);

                    editAction?.Invoke(editViewModel);
                })
            };

            categoryViewModel.DeleteCommand = new AsyncCommand(async ct =>
            {
                _categoryService.DeleteCategory(category);
            });

            return categoryViewModel;
        }

        public EditCategoryViewModel CreateAddCategoryViewModel()
        {
            Category newCategory = new();

            return CreateEditCategoryViewModel(newCategory, isCreated: false);
        }
        public EditCategoryViewModel CreateEditCategoryViewModel(Category category)
        {
            return CreateEditCategoryViewModel(category, isCreated: true);
        }

        private EditCategoryViewModel CreateEditCategoryViewModel(Category category, bool isCreated)
        {
            var possibleParents = _categoryService.GetCategories(CategoryFlags.Real).GetOrNull()
                .Where(c => c.Id != category.Id);

            var editCategoryViewModel = new EditCategoryViewModel()
            {
                Category = category,
                IsCreated = isCreated,
                PossibleParents = possibleParents
            };

            editCategoryViewModel.ApplyCommand = new AsyncCommand(async ct =>
            {
                if (!editCategoryViewModel.Validate(_categoryService))
                {
                    return;
                }

                if (!_categoryService.UpdateCategory(editCategoryViewModel.Category))
                {
                    return;
                }

                if (editCategoryViewModel.AssignTransactions)
                {
                    // Call method to assign transactions
                }
            });

            return editCategoryViewModel;
        }

        ICategoryService _categoryService;
    }
}
