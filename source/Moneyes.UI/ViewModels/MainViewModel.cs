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

namespace Moneyes.UI.ViewModels
{

    class MainViewModel : ViewModelBase
    {
        public ICommand FetchOnlineCommand { get; }
        public ICommand SelectCategoryCommand { get; }

        LiveDataService _liveDataService;
        IExpenseIncomeService _expenseIncomeService;
        TransactionRepository _transactionRepository;

        IRepository<AccountDetails> _accountStore;
        AccountDetails _selectedAccount;
        IList _selectedCategories;

        private ObservableCollection<AccountDetails> _accounts = new();
        private ObservableCollection<Transaction> _transactions = new();
        private ObservableCollection<CategoryViewModel> _categories = new();

        public IList SelectedCategories
        {
            get => _selectedCategories;
            set
            {
                _selectedCategories = value;
                OnPropertyChanged(nameof(SelectedCategories));
            }
        }

        public AccountDetails SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                _selectedAccount = value;
                OnPropertyChanged(nameof(SelectedAccount));
                FetchTransactions();
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

        public MainViewModel(
            LiveDataService liveDataService,
            IExpenseIncomeService expenseIncomeService,
            TransactionRepository transactionService,
            IRepository<AccountDetails> accountStore)
        {
            DisplayName = "Overview";

            _liveDataService = liveDataService;
            _accountStore = accountStore;
            _expenseIncomeService = expenseIncomeService;
            _transactionRepository = transactionService;

            FetchOnlineCommand = new AsyncCommand(async ct =>
            {
                Result<int> result = await _liveDataService
                    .FetchOnlineTransactions(SelectedAccount);
                    
                if (result.IsSuccessful && result.Data > 0)
                {
                    FetchTransactions();
                }
            });

            SelectCategoryCommand = new AsyncCommand<IEnumerable<CategoryViewModel>>(async (viewModels, ct) =>
            {
                await Task.Run(() => FetchTransactions(updateCategories: false));
            });

            FetchAccounts()
                .ContinueWith(t => FetchTransactions(), TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void FetchTransactions(bool updateCategories = true)
        {
            Category[] selectedCategories = SelectedCategories?
                .Cast<CategoryViewModel>()
                .Select(c => c.Category)
                .ToArray();

            // Get all transactions for selected category and filter
            IEnumerable<Transaction> transactions = _transactionRepository.All(
                filter: GetTransactionFilter(),
                categories: selectedCategories);

            Transactions = new(transactions);

            if (updateCategories)
            {
                UpdateCategories();
            }
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
                    Categories.Clear();

                    foreach ((Category category, decimal amt) in expenses)
                    {
                        Categories.Add(new(category, amt));
                    }

                    // Get total expenses
                    _expenseIncomeService.GetTotalExpense(SelectedAccount)
                        .OnSuccess(totalAmt =>
                        {
                            Categories.Add(new("Total", totalAmt));
                        })
                        .OnError(() => HandleError("Could not get total expense"));
                });
        }

        private void HandleError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async Task FetchAccounts()
        {
            Accounts = new(await _accountStore.GetAll());

            if (Accounts.Any())
            {
                return;
            }

            Result result = await _liveDataService.FetchAndImportAccounts();

            if (!result.IsSuccessful)
            {
                // TODO: Display message
            }

            Accounts = new(await _accountStore.GetAll());
        }
    }
}
