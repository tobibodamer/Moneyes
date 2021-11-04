using Moneyes.Core;
using Moneyes.LiveData;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Moneyes.UI.ViewModels
{
    class ExpenseCategoriesViewModel : CategoriesViewModel
    {
        CategoryViewModelFactory _factory;
        IExpenseIncomeService _expenseIncomeService;
        public ExpenseCategoriesViewModel(CategoryViewModelFactory factory,
            ICategoryService categoryService,
            IExpenseIncomeService expenseIncomeService)
            : base(factory, categoryService)
        {
            _expenseIncomeService = expenseIncomeService;
            _factory = factory;
        }

        private CategoryExpenseViewModel _selectedCategory;
        public CategoryExpenseViewModel SelectedCategory
        {
            get
            {
                return _selectedCategory;
            }
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<CategoryExpenseViewModel> _categories = new();
        public new ObservableCollection<CategoryExpenseViewModel> Categories
        {
            get
            {
                return _categories;
            }
            set
            {
                base.Categories = new(value);
                _categories = value;
                OnPropertyChanged();
            }
        }

        public override void UpdateCategories()
        {
            throw new NotImplementedException();
        }

        public void UpdateCategories(AccountDetails account)
        {
            // Get expenses per category
            _expenseIncomeService.GetExpensePerCategory(account)
                .OnError(() => { })//HandleError("Could not get expenses for this category"))
                .OnSuccess(expenses =>
                {
                    int? selectedCategoryId = SelectedCategory?.Category?.Id;

                    Categories.Clear();

                    foreach ((Category category, decimal amt) in expenses)
                    {
                        AddEntry(category, amt);
                    }

                    // Get total expenses
                    _expenseIncomeService.GetTotalExpense(account)
                                    .OnSuccess(totalAmt =>
                                    {
                                        AddEntry(Category.AllCategory, totalAmt);
                                    })
                                    .OnError(() => { }); //HandleError("Could not get total expense"));

                    // Set sub categories
                    foreach (CategoryExpenseViewModel category in Categories)
                    {
                        Category parent = category.Category?.Parent;
                        if (parent == null) { continue; }

                        // Add category as sub category in parent
                        Categories.FirstOrDefault(c => c.Category.Equals(parent))
                            .SubCatgeories.Add(category);
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
                            allCategory.IsSelected = true;
                            OnPropertyChanged(nameof(SelectedCategory));
                        }
                    }
                });
        }
        public void AddEntry(Category category, decimal expense)
        {
            Categories.Add(
                new CategoryExpenseViewModel(category, expense)
                {
                    AssignToTransaction = new AsyncCommand<Transaction>(async (transaction, ct) =>
                    {
                        Category targetCategory = category;
                        Category currentCategory = SelectedCategory?.Category;

                        if (TransactionService.MoveToCategory(transaction, currentCategory, targetCategory))
                        {
                            //UpdateCategories();
                            //UpdateTransactions();
                        }

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
                        if (transaction.Categories.Contains(targetCategory)) { return false; }

                        return true;
                    }),
                    EditCommand = new AsyncCommand(async ct =>
                    {
                        EditCategoryViewModel = _factory.CreateEditCategoryViewModel(category);
                    }),
                    DeleteCommand = new AsyncCommand(async ct =>
                    {
                        CategoryService.DeleteCategory(category);
                    })
                });
        }

        //public void UpdateCategories(IEnumerable<(Category category, decimal expense)> categories)
        //{
        //    foreach ((Category category, decimal expense) in categories)
        //    {
        //        Categories.Add(
        //            new CategoryExpenseViewModel(category, expense)
        //            {
        //                AssignToTransaction = new AsyncCommand<Transaction>(async (transaction, ct) =>
        //                {
        //                    Category targetCategory = category;
        //                    Category currentCategory = SelectedCategory?.Category;

        //                    if (TransactionService.MoveToCategory(transaction, currentCategory, targetCategory))
        //                    {
        //                        UpdateCategories();
        //                        UpdateTransactions();
        //                    }

        //                    await Task.CompletedTask;
        //                },
        //                (transaction) =>
        //                {
        //                    Category targetCategory = category;

        //                    // cant change null transaction
        //                    if (transaction == null) { return false; }

        //                    // cant add to 'All' category
        //                    if (targetCategory == Category.AllCategory) { return false; }

        //                    // cant add to own category
        //                    if (transaction.Categories.Contains(targetCategory)) { return false; }

        //                    return true;
        //                }),
        //                EditCommand = new AsyncCommand(async ct =>
        //                {
        //                    Category targetCategory = category;

        //                    var editCategoryViewModel = new EditCategoryViewModel();

        //                    editCategoryViewModel.ApplyCommand = new AsyncCommand(async ct =>
        //                    {
        //                        if (!editCategoryViewModel.Validate(_))
        //                        {
        //                            return;
        //                        }

        //                        if (!categoryService.UpdateCategory(Category))
        //                        {
        //                            return;
        //                        }

        //                        if (AssignTransactions)
        //                        {

        //                        }
        //                    });


        //                    //_editCategoryDialogService.ShowDialog(EditCategory);
        //                })
        //            });
        //    }
        //}
    }
}
