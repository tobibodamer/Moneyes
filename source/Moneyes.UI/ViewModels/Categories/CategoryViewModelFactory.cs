using Moneyes.Core;
using Moneyes.Data;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Moneyes.UI.ViewModels
{
    internal class CategoryViewModelFactory
    {
        private readonly ICategoryService _categoryService;
        public CategoryViewModelFactory(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }
        public CategoryViewModel CreateCategoryViewModel(Category category, Action<EditCategoryViewModel> editAction)
        {
            CategoryViewModel categoryViewModel = new()
            {
                Category = category,
                EditCommand = new RelayCommand(() =>
                {
                    EditCategoryViewModel editViewModel = CreateEditCategoryViewModel(category);

                    editAction?.Invoke(editViewModel);
                }),
                ReassignCommand = new RelayCommand(() =>
                {
                    _categoryService.AssignCategory(category);
                }),
                DeleteCommand = new RelayCommand(() =>
                {
                    _categoryService.DeleteCategory(category);
                }, () => !category.Idquals(Category.AllCategory) && !category.Idquals(Category.NoCategory))
            };

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
            List<Category> possibleParents = _categoryService.GetCategories(CategoryTypes.Real)
                .Where(c => !c.Idquals(category)).ToList();

            EditCategoryViewModel editCategoryViewModel = new(_categoryService)
            {
                PossibleParents = possibleParents,
                Category = category,
                IsCreated = isCreated,
                AssignTransactions = !isCreated,
                CanReassign = category != Category.AllCategory && category != Category.NoCategory
            };

            return editCategoryViewModel;
        }
    }
}
