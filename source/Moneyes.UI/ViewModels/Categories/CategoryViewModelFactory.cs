using Moneyes.Core;
using Moneyes.Data;
using Moneyes.LiveData;
using Moneyes.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Moneyes.UI.ViewModels
{
    internal class CategoryViewModelFactory
    {
        private readonly ICategoryService _categoryService;
        private readonly IStatusMessageService _statusMessageService;
        public CategoryViewModelFactory(ICategoryService categoryService, IStatusMessageService statusMessageService)
        {
            _categoryService = categoryService;
            _statusMessageService = statusMessageService;
        }
        public CategoryViewModel CreateCategoryViewModel(Category category,
            Action<EditCategoryViewModel> editCallback)
        {
            CategoryViewModel categoryViewModel = new(_categoryService, _statusMessageService);

            InitViewModel(category, categoryViewModel, editCallback);

            return categoryViewModel;
        }

        private void InitViewModel<TCategoryViewModel>(
            Category category,
            TCategoryViewModel categoryViewModel,
            Action<EditCategoryViewModel> editCallback)
            where TCategoryViewModel : CategoryViewModel
        {
            List<Category> possibleParents = _categoryService.GetCategories(CategoryTypes.Real)
                .Where(c => !c.Id.Equals(category.Id)).ToList();

            bool canAssign(Transaction transaction)
            {
                Category targetCategory = category;

                // cant change null transaction
                if (transaction == null) { return false; }

                // cant add to 'All' category
                if (targetCategory == Category.AllCategory) { return false; }

                // cant add to own category
                bool isOwn = transaction.Category?.Id == targetCategory.Id;

                return !isOwn;
            };

            categoryViewModel.Category = category;
            categoryViewModel.PossibleParents = possibleParents;
            categoryViewModel.IsCreated = true;
            categoryViewModel.AssignTransactions = false;
            categoryViewModel.CanReassign = category.IsReal;

            categoryViewModel.EditCommand = new RelayCommand(() =>
            {
                editCallback?.Invoke(CreateEditCategoryViewModel(category));
            });

            categoryViewModel.ReassignCommand = new RelayCommand(() =>
            {
                Category category = categoryViewModel.Category;

                int reassignedCount = _categoryService.AssignCategory(category);

                if (reassignedCount > 0)
                {
                    _statusMessageService.ShowMessage($"{reassignedCount} transaction(s) reassigned");
                }
            }, () => categoryViewModel.CanReassign);

            //categoryViewModel.SaveCommand = new RelayCommand(() =>
            //{
            //    if (!categoryViewModel.Validate())
            //    {
            //        return;
            //    }

            //    Category category = categoryViewModel.Category;

            //    if (_categoryService.UpdateCategory(category))
            //    {
            //        _statusMessageService.ShowMessage($"Category '{category.Name}' created");
            //    }
            //    else
            //    {
            //        _statusMessageService.ShowMessage($"Category '{category.Name}' saved");
            //    }

            //    if (categoryViewModel.CanReassign && categoryViewModel.AssignTransactions)
            //    {
            //        categoryViewModel.ReassignCommand.Execute(null);
            //    }
            //});

            categoryViewModel.DeleteCommand = new RelayCommand(() =>
            {
                Category category = categoryViewModel.Category;

                if (_categoryService.DeleteCategory(category))
                {
                    _statusMessageService.ShowMessage($"Category '{category.Name}' deleted");
                }
            }, () => !category.IsAllCategory() && !category.IsNoCategory());

            categoryViewModel.MoveToCategory = new RelayCommand<TransactionViewModel>(transaction =>
            {
                Category targetCategory = categoryViewModel.Category;

                if (_categoryService.MoveToCategory(transaction.Transaction, targetCategory))
                {
                    _statusMessageService.ShowMessage($"Moved to '{targetCategory.Name}'");
                }
            }, t => categoryViewModel.IsCreated && canAssign(t.Transaction));
        }

        public EditCategoryViewModel CreateAddCategoryViewModel()
        {
            Category newCategory = new(Guid.NewGuid());

            return CreateEditCategoryViewModel(newCategory, isCreated: false);
        }
        public EditCategoryViewModel CreateEditCategoryViewModel(Category category)
        {
            return CreateEditCategoryViewModel(category, isCreated: true);
        }

        private EditCategoryViewModel CreateEditCategoryViewModel(Category category, bool isCreated)
        {
            List<Category> possibleParents = _categoryService.GetCategories(CategoryTypes.Real)
                .Where(c => !c.Id.Equals(category.Id)).ToList();

            EditCategoryViewModel editCategoryViewModel = new(_categoryService, _statusMessageService)
            {
                PossibleParents = possibleParents,
                Category = category,
                IsCreated = isCreated,
                AssignTransactions = !isCreated,
                CanReassign = category != Category.AllCategory && category != Category.NoCategory
            };

            return editCategoryViewModel;
        }

        public CategoryExpenseViewModel CreateCategoryExpenseViewModel(
            Category category,
            Expenses expenses,
            Action<EditCategoryViewModel> editCallback)
        {
            CategoryExpenseViewModel categoryViewModel = new(category, expenses, _categoryService, _statusMessageService);

            InitViewModel(category, categoryViewModel, editCallback);

            return categoryViewModel;
        }
    }
}
