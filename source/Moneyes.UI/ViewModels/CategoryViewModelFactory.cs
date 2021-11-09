using Moneyes.Core;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;
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
                }),
                ReassignCommand = new AsyncCommand(async ct =>
                {
                    _categoryService.AssignCategory(category);
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
            List<Category> possibleParents = _categoryService.GetCategories(CategoryFlags.Real).GetOrNull()
                .Where(c => c.Id != category.Id).ToList();

            // Add no category to select no category
            //possibleParents.Insert(0, Category.NoCategory);

            var editCategoryViewModel = new EditCategoryViewModel()
            {
                PossibleParents = possibleParents,
                Category = category,
                IsCreated = isCreated,                
                AssignTransactions = !isCreated
            };

            editCategoryViewModel.ApplyCommand = new AsyncCommand(async ct =>
            {
                if (!editCategoryViewModel.Validate(_categoryService))
                {
                    return;
                }

                Category category = editCategoryViewModel.Category;

                _categoryService.UpdateCategory(category);
                
                
                if (editCategoryViewModel.AssignTransactions)
                {
                    // Call method to assign transactions
                    _categoryService.AssignCategory(category);
                }
            });

            return editCategoryViewModel;
        }

        ICategoryService _categoryService;
    }
}
