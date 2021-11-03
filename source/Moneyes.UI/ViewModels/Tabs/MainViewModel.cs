using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System;
using Moneyes.LiveData;
using Moneyes.Core;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using Moneyes.Core.Filters;
using Moneyes.Data;
using System.Windows;
using System.Collections;
using System.Threading;
using Moneyes.UI.View;
using Moneyes.UI.Services;

namespace Moneyes.UI.ViewModels
{
    class MainViewModel : ViewModelBase, ITabViewModel
    {
        private LiveDataService _liveDataService;
        private IExpenseIncomeService _expenseIncomeService;
        private readonly ITransactionService _transactionService;
        private readonly IBankingService _bankingService;
        private readonly IStatusMessageService _statusMessageService;
        private readonly IDialogService<EditCategoryViewModel> _editCategoryDialogService;
        private AccountDetails _selectedAccount;
        private CategoryExpenseViewModel _selectedCategory;
        private Balance _currentBalance;

        private ObservableCollection<AccountDetails> _accounts = new();
        private ObservableCollection<Transaction> _transactions = new();
        //private ObservableCollection<CategoryExpenseViewModel> _categories = new();

        #region UI Properties
        public ICommand LoadedCommand { get; }
        public ICommand FetchOnlineCommand { get; }
        public ICommand SelectCategoryCommand { get; }


        public ExpenseCategoriesViewModel ExpenseCategories { get; }
        //public CategoryExpenseViewModel SelectedCategory
        //{
        //    get => Categories.FirstOrDefault(c => c.IsSelected);
        //    //set
        //    //{
        //    //    foreach (var c in Categories)
        //    //    {

        //    //    }
        //    //    value.IsSelected = true;
        //    //    OnPropertyChanged(nameof(SelectedCategory));
        //    //}
        //}

        public AccountDetails SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                _selectedAccount = value;
                OnPropertyChanged();
                UpdateCategories();
                UpdateTransactions();
            }
        }

        public ObservableCollection<AccountDetails> Accounts
        {
            get => _accounts;
            set
            {
                _accounts = value;

                if (value != null && value.Any())
                {
                    SelectedAccount = value.First();
                }

                OnPropertyChanged();
            }
        }

        //public ObservableCollection<CategoryExpenseViewModel> Categories
        //{
        //    get => _categories;
        //    set
        //    {
        //        _categories = value;
        //        OnPropertyChanged();
        //    }
        //}

        public ObservableCollection<Transaction> Transactions
        {
            get => _transactions;
            set
            {
                _transactions = value;
                OnPropertyChanged();
            }
        }

        public Balance CurrentBalance
        {
            get => _currentBalance;
            set
            {
                _currentBalance = value;
                OnPropertyChanged();
            }
        }
        //private EditCategoryViewModel _editCategory;
        //public EditCategoryViewModel EditCategory
        //{
        //    get => _editCategory;
        //    set
        //    {
        //        _editCategory = value;
        //        OnPropertyChanged();
        //    }
        //}

        #endregion
        public MainViewModel(
            LiveDataService liveDataService,
            IExpenseIncomeService expenseIncomeService,
            ITransactionService transactionService,
            IBankingService bankingService,
            IStatusMessageService statusMessageService,
            IDialogService<EditCategoryViewModel> editCategoryDialogService,
            ExpenseCategoriesViewModel expenseCategoriesViewModel,
            ICategoryService categoryService)
        {
            DisplayName = "Transactions";
            ExpenseCategories = expenseCategoriesViewModel;
            _liveDataService = liveDataService;
            _expenseIncomeService = expenseIncomeService;
            _transactionService = transactionService;
            _bankingService = bankingService;
            _statusMessageService = statusMessageService;
            this._editCategoryDialogService = editCategoryDialogService;
            LoadedCommand = new AsyncCommand(async ct =>
            {
                if (!bankingService.HasBankingDetails)
                {
                    // No bank connection configured -> show message?
                    return;
                }

                Accounts = new(_bankingService.GetAccounts());

                if (Accounts.Any())
                {
                    return;
                }
            });

            FetchOnlineCommand = new AsyncCommand(async ct =>
            {
                Result<int> result = await _liveDataService
                    .FetchOnlineTransactionsAndBalances(SelectedAccount);

                if (result.IsSuccessful)
                {
                    if (result.Data == 0)
                    {
                        _statusMessageService.ShowMessage($"No new transactions available.");
                        return;
                    }

                    UpdateCategories();
                    UpdateTransactions();

                    _statusMessageService.ShowMessage($"Fetched {result.Data} transactions.");
                }
            });

            ExpenseCategories.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(ExpenseCategories.SelectedCategory))
                {
                    Task.Run(() => UpdateTransactions());
                }
            };

            //SelectCategoryCommand = new AsyncCommand<CategoryExpenseViewModel>(async (viewModel, ct) =>
            //{
            //    await Task.Run(() => UpdateTransactions());
            //    OnPropertyChanged(nameof(SelectedCategory));
            //});

            categoryService.CategoryChanged += (c) =>
            {
                UpdateCategories();
                UpdateTransactions();
            };
        }

        private void UpdateTransactions()
        {
            //Category[] selectedCategories = SelectedCategory?
            //    .Cast<CategoryViewModel>()
            //    .Select(c => c.Category)
            //    .ToArray();

            Category selectedCategory = ExpenseCategories.SelectedCategory?.Category;

            // Get all transactions for selected category and filter
            IEnumerable<Transaction> transactions = _transactionService.GetTransactions(
                filter: GetTransactionFilter(),
                categories: selectedCategory);

            CurrentBalance = _bankingService.GetBalance(DateTime.Now, SelectedAccount);

            Transactions = new(transactions);
        }

        private TransactionFilter GetTransactionFilter()
        {
            return new TransactionFilter()
            {
                AccountNumber = _selectedAccount.Number
            };
        }

        private void UpdateCategories()
        {
            ExpenseCategories.UpdateCategories(SelectedAccount);

            //// Get expenses per category
            //_expenseIncomeService.GetExpensePerCategory(SelectedAccount)
            //    .OnError(() => HandleError("Could not get expenses for this category"))
            //    .OnSuccess(expenses =>
            //    {
            //        int? selectedCategoryId = SelectedCategory?.Category?.Id;

            //        ExpenseCategories.Categories.Clear();

            //        foreach ((Category category, decimal amt) in expenses)
            //        {
            //            ExpenseCategories.Add(category, amt);
            //        }

            //        // Get total expenses
            //        _expenseIncomeService.GetTotalExpense(SelectedAccount)
            //                        .OnSuccess(totalAmt =>
            //                        {
            //                            ExpenseCategories.Add(Category.AllCategory, totalAmt);
            //                        })
            //                        .OnError(() => HandleError("Could not get total expense"));

            //        // Set sub categories
            //        foreach (CategoryExpenseViewModel category in Categories)
            //        {
            //            Category parent = category.Category?.Parent;
            //            if (parent == null) { continue; }

            //            // Add category as sub category in parent
            //            Categories.FirstOrDefault(c => c.Category.Equals(parent))
            //                .SubCatgeories.Add(category);
            //        }

            //        if (selectedCategoryId.HasValue)
            //        {
            //            CategoryExpenseViewModel previouslySelectedCategory = Categories
            //                .FirstOrDefault(c => c.Category.Id == selectedCategoryId);

            //            if (previouslySelectedCategory != null)
            //            {
            //                previouslySelectedCategory.IsSelected = true;
            //            }
            //        }
            //        else
            //        {
            //            CategoryExpenseViewModel allCategory = Categories
            //                .FirstOrDefault(c => c.Category == Category.AllCategory);

            //            if (allCategory != null)
            //            {
            //                allCategory.IsSelected = true;
            //                OnPropertyChanged(nameof(SelectedCategory));
            //            }
            //        }
            //    });
        }

        //private void UpdateCategories()
        //{
        //    // Get expenses per category
        //    _expenseIncomeService.GetExpensePerCategory(SelectedAccount)
        //        .OnError(() => HandleError("Could not get expenses for this category"))
        //        .OnSuccess(expenses =>
        //        {
        //            int? selectedCategoryId = SelectedCategory?.Category?.Id;

        //            Categories.Clear();

        //            foreach ((Category category, decimal amt) in expenses)
        //            {
        //                Categories.Add(
        //                    new CategoryExpenseViewModel(category, amt)
        //                    {
        //                        AssignToTransaction = new AsyncCommand<Transaction>(async (transaction, ct) =>
        //                        {
        //                            Category targetCategory = category;
        //                            Category currentCategory = SelectedCategory?.Category;

        //                            if (_transactionService.MoveToCategory(transaction, currentCategory, targetCategory))
        //                            {
        //                                UpdateCategories();
        //                                UpdateTransactions();
        //                            }

        //                            await Task.CompletedTask;
        //                        },
        //                        (transaction) =>
        //                        {
        //                            Category targetCategory = category;

        //                            // cant change null transaction
        //                            if (transaction == null) { return false; }

        //                            // cant add to 'All' category
        //                            if (targetCategory == Category.AllCategory) { return false; }

        //                            // cant add to own category
        //                            if (transaction.Categories.Contains(targetCategory)) { return false; }

        //                            return true;
        //                        }),
        //                        EditCommand = new AsyncCommand(async ct =>
        //                        {
        //                            Category targetCategory = category;

        //                            var editCategoryViewModel = new EditCategoryViewModel();

        //                            editCategoryViewModel.ApplyCommand = new AsyncCommand(async ct =>
        //                            {
        //                                if (!editCategoryViewModel.Validate(_))
        //                                {
        //                                    return;
        //                                }

        //                                if (!categoryService.UpdateCategory(Category))
        //                                {
        //                                    return;
        //                                }

        //                                if (AssignTransactions)
        //                                {

        //                                }
        //                            });


        //                            //_editCategoryDialogService.ShowDialog(EditCategory);
        //                        })
        //                    });
        //            }

        //            // Get total expenses
        //            _expenseIncomeService.GetTotalExpense(SelectedAccount)
        //                            .OnSuccess(totalAmt =>
        //                            {
        //                                Categories.Add(new(Category.AllCategory, totalAmt));
        //                            })
        //                            .OnError(() => HandleError("Could not get total expense"));

        //            // Set sub categories
        //            foreach (CategoryExpenseViewModel category in Categories)
        //            {
        //                Category parent = category.Category?.Parent;
        //                if (parent == null) { continue; }

        //                // Add category as sub category in parent
        //                Categories.FirstOrDefault(c => c.Category.Equals(parent))
        //                    .SubCatgeories.Add(category);
        //            }

        //            if (selectedCategoryId.HasValue)
        //            {
        //                CategoryExpenseViewModel previouslySelectedCategory = Categories
        //                    .FirstOrDefault(c => c.Category.Id == selectedCategoryId);

        //                if (previouslySelectedCategory != null)
        //                {
        //                    previouslySelectedCategory.IsSelected = true;
        //                }
        //            }
        //            else
        //            {
        //                CategoryExpenseViewModel allCategory = Categories
        //                    .FirstOrDefault(c => c.Category == Category.AllCategory);

        //                if (allCategory != null)
        //                {
        //                    allCategory.IsSelected = true;
        //                    OnPropertyChanged(nameof(SelectedCategory));
        //                }
        //            }
        //        });
        //}

        private void HandleError(string message)
        {
            _statusMessageService.ShowMessage($"Error: {message}");
            //MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
