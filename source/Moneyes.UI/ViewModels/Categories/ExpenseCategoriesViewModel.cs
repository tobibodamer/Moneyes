﻿using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using Moneyes.LiveData;
using Moneyes.UI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Moneyes.UI.ViewModels
{
    class ExpenseCategoriesViewModel : CategoriesViewModelBase<CategoryExpenseViewModel>
    {
        CategoryViewModelFactory _factory;
        IExpenseIncomeService _expenseIncomeService;
        ICategoryService _categoryService;
        IStatusMessageService _statusMessageService;

        public ExpenseCategoriesViewModel(CategoryViewModelFactory factory,
            ICategoryService categoryService,
            IExpenseIncomeService expenseIncomeService,
            IStatusMessageService statusMessageService)
            : base(factory)
        {
            _expenseIncomeService = expenseIncomeService;
            _categoryService = categoryService;
            _statusMessageService = statusMessageService;
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

        /// <summary>
        /// Dynamically updates the list of category viewmodels by inserting new and updating existing entries.
        /// </summary>
        /// <param name="categorieExpenses"></param>
        private void UpdateCategories(IList<CategoryExpenseViewModel> categorieExpenses,
            IComparer<CategoryExpenseViewModel> comparer)
        {
            var previouslySelectedCategory = SelectedCategory?.Category;

            Categories.DynamicUpdate(
                categorieExpenses,
                (c1, c2) => c1.Category.Idquals(c2.Category),
                comparer);

            if (previouslySelectedCategory != null)
            {
                SelectCategory(previouslySelectedCategory);
            }

            if (SelectedCategory is null)
            {
                SelectCategory(Category.AllCategory);
            }

            OnPropertyChanged(nameof(Categories));
        }

        /// <summary>
        /// Select the given category using the default ID selector.
        /// </summary>
        /// <param name="category"></param>
        public void SelectCategory(Category category)
        {
            var categoryExpense = Categories
                .FirstOrDefault(c => c.Category != null && c.Category.Idquals(category));

            SelectedCategory = categoryExpense;
        }

        /// <summary>
        /// Updates the category expenses by reloading them using the given <paramref name="filter"/>.
        /// </summary>
        /// <param name="filter"></param>
        public void UpdateCategories(TransactionFilter filter, CategoryFlags categoryFlags = CategoryFlags.All,
            bool flat = false, bool order = false)
        {
            // Get expenses per category
            _expenseIncomeService.GetExpensePerCategory(filter, categoryFlags.HasFlag(CategoryFlags.NoCategory),
                includeSubCategories: true)
                .OnError(() =>
                {
                    _statusMessageService.ShowMessage("Could not get expenses of categories", "Retry",
                        () => UpdateCategories(filter, categoryFlags, flat, order));
                })
                .OnSuccess(expenses =>
                {
                    List<CategoryExpenseViewModel> categories = new();

                    categories.AddRange(
                        expenses.Select(exp => CreateEntry(exp.Category, exp.TotalAmt))
                    );

                    if (!flat)
                    {
                        // Set sub categories
                        SetSubCategories(categories);
                    }

                    if (categoryFlags.HasFlag(CategoryFlags.AllCategory))
                    {
                        // Get total expenses
                        _expenseIncomeService.GetTotalExpense(filter)
                                        .OnSuccess(totalAmt =>
                                        {
                                            var allCategory = CreateEntry(Category.AllCategory, totalAmt);

                                            categories.Add(allCategory);
                                        })
                                        .OnError(() =>
                                        {
                                            _statusMessageService.ShowMessage("Could not get total expense");
                                        });
                    }
                    UpdateCategories(categories, order ? new ExpenseComparer() : new CategoryComparer());
                });
        }

        private CategoryExpenseViewModel CreateEntry(Category category, decimal expense)
        {
            bool canAssign(Transaction transaction)
            {
                Category targetCategory = category;

                // cant change null transaction
                if (transaction == null) { return false; }

                // cant add to 'All' category
                if (targetCategory == Category.AllCategory) { return false; }

                // cant add to own category
                var isOwn = transaction.Categories.Contains(targetCategory);

                return !isOwn;
            };

            return new CategoryExpenseViewModel(category, expense)
            {
                MoveToCategory = new RelayCommand<Transaction>(transaction =>
                {
                    Category targetCategory = category;
                    Category currentCategory = SelectedCategory?.Category;

                    _categoryService.MoveToCategory(transaction, currentCategory, targetCategory);
                }, canAssign),
                CopyToCategory = new RelayCommand<Transaction>(transaction =>
                {
                    Category targetCategory = category;                    
                    
                    _categoryService.AddToCategory(transaction, targetCategory);
                }, transaction =>
                {
                    Category targetCategory = category;
                    Category currentCategory = SelectedCategory?.Category;

                    if (targetCategory.IsExlusive || currentCategory.IsExlusive)
                    {
                        return false;
                    }

                    return canAssign(transaction);
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

        class CategoryComparer : IComparer<CategoryExpenseViewModel>
        {
            public int Compare(CategoryExpenseViewModel x, CategoryExpenseViewModel y)
            {
                if (x.IsNoCategory)
                {
                    return -1;
                }

                if (y.IsNoCategory)
                {
                    return 1;
                }

                if (x.Category.Idquals(Category.AllCategory))
                {
                    return 1;
                }

                if (y.Category.Idquals(Category.AllCategory))
                {
                    return -1;
                }

                return x.Category.Name.CompareTo(y.Category.Name);
            }
        }

        class ExpenseComparer : IComparer<CategoryExpenseViewModel>
        {
            public int Compare(CategoryExpenseViewModel x, CategoryExpenseViewModel y)
            {
                if (x.Target > 0 && y.Target == 0)
                {
                    return -1;
                }
                else if (x.Target == 0 && y.Target > 0)
                {
                    return 1;
                }

                return x.TotalExpense.CompareTo(y.TotalExpense) * -1;
            }
        }
    }
}
