using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Moneyes.UI.ViewModels
{
    class ExpenseCategoriesViewModel : CategoriesViewModelBase<CategoryExpenseViewModel>
    {
        CategoryViewModelFactory _factory;
        IExpenseIncomeService _expenseIncomeService;
        ICategoryService _categoryService;
        public ExpenseCategoriesViewModel(CategoryViewModelFactory factory,
            ICategoryService categoryService,
            IExpenseIncomeService expenseIncomeService)
            : base(factory)
        {
            _expenseIncomeService = expenseIncomeService;
            _categoryService = categoryService;
            _factory = factory;
        }

        private CategoryExpenseViewModel _selectedCategory;
        public CategoryExpenseViewModel SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory == value)
                {
                    return;
                }

                _selectedCategory = value;
                OnPropertyChanged();
            }
        }

        private void SetSubCategories(List<CategoryExpenseViewModel> flatCategories)
        {
            // Select all categories with a matching parent
            var categoriesWithParent = flatCategories.Where(
                c => c.Parent is not null &&
                flatCategories.Select(c => c.Category).Contains(c.Category)).ToList();

            // Add categories with parent to sub categories of parent
            foreach (var category in categoriesWithParent)
            {
                var parent = category.Parent;
                var parentCategoryViewModel = flatCategories.FirstOrDefault(c => c.Category.Idquals(parent));

                parentCategoryViewModel.SubCatgeories.Add(category);
            }

            // Remove all categories with a parent (from top level)
            flatCategories.RemoveAll(c => categoriesWithParent.Contains(c));
        }

        public void UpdateCategories(AccountDetails account, DateTime? startDate = null, DateTime? endDate = null)
        {
            TransactionFilter filter = new()
            {
                AccountNumber = account.Number,
                StartDate = startDate,
                EndDate = endDate
            };

            // Get expenses per category
            _expenseIncomeService.GetExpensePerCategory(filter)
                .OnError(() => { })//HandleError("Could not get expenses for this category"))
                .OnSuccess(expenses =>
                {
                    int? selectedCategoryId = SelectedCategory?.Category?.Id;

                    List<CategoryExpenseViewModel> categories = new();

                    foreach ((Category category, decimal amt) in expenses)
                    {
                        CategoryExpenseViewModel categoryViewModel = CreateEntry(category, amt);

                        categories.Add(categoryViewModel);
                    }

                    // Set sub categories
                    SetSubCategories(categories);


                    // Get total expenses
                    _expenseIncomeService.GetTotalExpense(filter)
                                    .OnSuccess(totalAmt =>
                                    {
                                        var allCategory = CreateEntry(Category.AllCategory, totalAmt);
                                        categories.Add(allCategory);
                                    })
                                    .OnError(() => { }); //HandleError("Could not get total expense"));


                    var categoriesToRemove = Categories
                        .Where(oldCategory => !categories.Any(c => c.Category.Idquals(oldCategory.Category)))
                        .ToList();

                    foreach (var category in categoriesToRemove)
                    {
                        Categories.Remove(category);
                    }

                    foreach (var category in categories)
                    {
                        Categories.AddOrUpdate(category, c => c.Category.Idquals(category.Category));
                    }


                    if (selectedCategoryId.HasValue)
                    {
                        CategoryExpenseViewModel previouslySelectedCategory = Categories
                            .FirstOrDefault(c => c.Category.Id == selectedCategoryId);

                        if (previouslySelectedCategory != null)
                        {
                            previouslySelectedCategory.IsSelected = true;
                        }
                    }
                    else
                    {
                        CategoryExpenseViewModel allCategory = Categories
                            .FirstOrDefault(c => c.Category == Category.AllCategory);

                        if (allCategory != null)
                        {
                            //allCategory.IsSelected = true;
                            SelectedCategory = allCategory;
                        }
                    }
                });
        }

        private CategoryExpenseViewModel CreateEntry(Category category, decimal expense)
        {
            return new CategoryExpenseViewModel(category, expense)
            {
                AssignToTransaction = new AsyncCommand<Transaction>(async (transaction, ct) =>
                {
                    Category targetCategory = category;
                    Category currentCategory = SelectedCategory?.Category;

                    _categoryService.MoveToCategory(transaction, currentCategory, targetCategory);

                    await Task.CompletedTask;
                },
                    (transaction) =>
                    {
                        Category targetCategory = category;

                        // cant change null transaction
                        if (transaction == null) { return false; }

                        // cant add to 'All' category
                        if (targetCategory == Category.AllCategory) { return false; }

                        // cant add to own category
                        return !transaction.Categories.Contains(targetCategory);
                    }),
                EditCommand = new AsyncCommand(async ct =>
                {
                    EditCategoryViewModel = _factory.CreateEditCategoryViewModel(category);
                }),
                DeleteCommand = new AsyncCommand(async ct =>
                {
                    _categoryService.DeleteCategory(category);
                }),
                ReassignCommand = new AsyncCommand(async ct =>
                {
                    _categoryService.AssignCategory(category);
                })
            };
        }
        public void AddEntry(Category category, decimal expense)
        {
            Categories.Add(CreateEntry(category, expense));
        }

        public bool IsSelected(Category category)
        {
            return SelectedCategory?.Category.Id == category.Id;
        }
    }
}
