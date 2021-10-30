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

namespace Moneyes.UI.ViewModels
{
    class MainViewModel : ViewModelBase, ITabViewModel
    {
        private LiveDataService _liveDataService;
        private IExpenseIncomeService _expenseIncomeService;
        private readonly ITransactionService _transactionService;
        private readonly IBankingService _bankingService;

        private AccountDetails _selectedAccount;
        private CategoryViewModel _selectedCategory;
        private Balance _currentBalance;

        private ObservableCollection<AccountDetails> _accounts = new();
        private ObservableCollection<Transaction> _transactions = new();
        private ObservableCollection<CategoryViewModel> _categories = new();

        #region UI Properties
        public ICommand LoadedCommand { get; }
        public ICommand FetchOnlineCommand { get; }
        public ICommand SelectCategoryCommand { get; }

        public CategoryViewModel SelectedCategory
        {
            get => Categories.FirstOrDefault(c => c.IsSelected);
            //set
            //{
            //    _selectedCategory = value;
            //    OnPropertyChanged(nameof(SelectedCategory));
            //}
        }

        public AccountDetails SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                _selectedAccount = value;
                OnPropertyChanged(nameof(SelectedAccount));
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

                OnPropertyChanged(nameof(Accounts));
            }
        }

        public ObservableCollection<CategoryViewModel> Categories
        {
            get => _categories;
            set
            {
                _categories = value;

                OnPropertyChanged(nameof(Accounts));
            }
        }

        public ObservableCollection<Transaction> Transactions
        {
            get => _transactions;
            set
            {
                _transactions = value;

                OnPropertyChanged(nameof(Transactions));
            }
        }

        public Balance CurrentBalance
        {
            get => _currentBalance;
            set
            {
                _currentBalance = value;

                OnPropertyChanged(nameof(CurrentBalance));
            }
        }

#endregion
        public MainViewModel(
            LiveDataService liveDataService,
            IExpenseIncomeService expenseIncomeService,
            ITransactionService transactionService,
            IBankingService bankingService)
        {
            DisplayName = "Transactions";

            _liveDataService = liveDataService;
            _expenseIncomeService = expenseIncomeService;
            _transactionService = transactionService;
            _bankingService = bankingService;

            LoadedCommand = new AsyncCommand(async ct =>
            {
                if (!bankingService.HasBankingDetails)
                {
                    // No bank connection configured -> show message?
                    return;
                }

                await FetchAccounts();

                //TODO: Remove
                //UpdateCategories();
            });

            FetchOnlineCommand = new AsyncCommand(async ct =>
            {
                Result<int> result = await _liveDataService
                    .FetchOnlineTransactionsAndBalances(SelectedAccount);

                if (result.IsSuccessful && result.Data > 0)
                {
                    UpdateCategories();
                    UpdateTransactions();
                }
            });

            SelectCategoryCommand = new AsyncCommand<CategoryViewModel>(async (viewModel, ct) =>
            {
                await Task.Run(() => UpdateTransactions());
                OnPropertyChanged(nameof(SelectedCategory));
            });
        }

        private void UpdateTransactions()
        {
            //Category[] selectedCategories = SelectedCategory?
            //    .Cast<CategoryViewModel>()
            //    .Select(c => c.Category)
            //    .ToArray();

            Category selectedCategory = SelectedCategory?.Category;

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
            // Get expenses per category
            _expenseIncomeService.GetExpensePerCategory(SelectedAccount)
                .OnError(() => HandleError("Could not get expenses for this category"))
                .OnSuccess(expenses =>
                {
                    int? selectedCategoryId = SelectedCategory?.Category?.Id;

                    Categories.Clear();

                    foreach ((Category category, decimal amt) in expenses)
                    {
                        Categories.Add(
                            new CategoryViewModel(category, amt)
                            {
                                AddToCategoryCommand = new AsyncCommand<Transaction>(async (transaction, ct) =>
                                {
                                    Category targetCategory = category;
                                    Category currentCategory = SelectedCategory?.Category;

                                    if (_transactionService.MoveToCategory(transaction, currentCategory, targetCategory))
                                    {
                                        UpdateCategories();
                                        UpdateTransactions();
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
                                })
                            });
                    }

                    // Get total expenses
                    _expenseIncomeService.GetTotalExpense(SelectedAccount)
                        .OnSuccess(totalAmt =>
                        {
                            Categories.Add(new(Category.AllCategory, totalAmt));
                        })
                        .OnError(() => HandleError("Could not get total expense"));

                    // Set sub categories
                    foreach (CategoryViewModel category in Categories)
                    {
                        Category parent = category.Category?.Parent;
                        if (parent == null) { continue; }

                        // Add category as sub category in parent
                        Categories.FirstOrDefault(c => c.Category.Equals(parent))
                            .SubCatgeories.Add(category);
                    }

                    if (selectedCategoryId.HasValue)
                    {
                        CategoryViewModel previouslySelectedCategory = Categories
                            .FirstOrDefault(c => c.Category.Id == selectedCategoryId);

                        if (previouslySelectedCategory != null)
                        {
                            previouslySelectedCategory.IsSelected = true;
                        }
                    }
                });
        }

        private void HandleError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async Task FetchAccounts()
        {
            Accounts = new(_bankingService.GetAccounts());

            if (Accounts.Any())
            {
                return;
            }

            Result result = await _liveDataService.FetchAndImportAccounts();

            if (!result.IsSuccessful)
            {
                // TODO: Display message
            }

            Accounts = new(_bankingService.GetAccounts());
        }
    }
}
