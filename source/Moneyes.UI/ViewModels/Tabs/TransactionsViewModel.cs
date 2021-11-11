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
using System.Diagnostics;

namespace Moneyes.UI.ViewModels
{
    class TransactionsViewModel : ViewModelBase, ITabViewModel
    {
        private LiveDataService _liveDataService;
        private readonly TransactionRepository _transactionRepository;
        private readonly IBankingService _bankingService;
        private readonly IStatusMessageService _statusMessageService;
        private AccountDetails _selectedAccount;
        private Balance _currentBalance;

        private ObservableCollection<AccountDetails> _accounts = new();
        private ObservableCollection<Transaction> _transactions = new();
        //private ObservableCollection<CategoryExpenseViewModel> _categories = new();

        #region UI Properties
        public ICommand LoadedCommand { get; }
        public ICommand FetchOnlineCommand { get; }
        public ICommand SelectCategoryCommand { get; }


        public ExpenseCategoriesViewModel ExpenseCategories { get; }

        public AccountDetails SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                _selectedAccount = value;                
                OnPropertyChanged();
                UpdateCategories();                
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
        private DateTime _fromDate;
        public DateTime FromDate
        {
            get => _fromDate;
            set
            {
                _fromDate = value;
                OnPropertyChanged();
            }
        }

        private DateTime _endDate;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged();
            }
        }

        private bool _loadingTransactions;
        public bool Loading
        {
            get => _loadingTransactions;
            set
            {
                _loadingTransactions = value;
                OnPropertyChanged();
            }
        }

        public ICommand DateChangedCommand { get; }

        #endregion
        public TransactionsViewModel(
            LiveDataService liveDataService,
            TransactionRepository transactionRepository,
            IBankingService bankingService,
            IStatusMessageService statusMessageService,
            ExpenseCategoriesViewModel expenseCategoriesViewModel,
            CategoryRepository categoryRepository)
        {
            DisplayName = "Transactions";
            ExpenseCategories = expenseCategoriesViewModel;
            _liveDataService = liveDataService;
            _transactionRepository = transactionRepository;
            _bankingService = bankingService;
            _statusMessageService = statusMessageService;

            LoadedCommand = new AsyncCommand(async ct =>
            {

            });

            FetchOnlineCommand = new AsyncCommand(async ct =>
            {
                Result<int> result = await _liveDataService
                    .FetchTransactionsAndBalances(SelectedAccount);

                if (result.IsSuccessful)
                {
                    if (result.Data == 0)
                    {
                        _statusMessageService.ShowMessage($"No new transactions available.");
                        return;
                    }

                    //UpdateCategories();
                    //UpdateTransactions();

                    _statusMessageService.ShowMessage($"Fetched {result.Data} new transactions.");
                }
            });

            // Date selection

            FromDate = new(DateTime.Now.Year, DateTime.Now.Month, 1); ;
            EndDate = DateTime.Now;

            DateChangedCommand = new AsyncCommand(async ct =>
            {
                UpdateCategories();
                UpdateTransactions();
            });

            ExpenseCategories.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(ExpenseCategories.SelectedCategory))
                {
                    UpdateTransactions();
                }
            };

            categoryRepository.EntityUpdated += (category) =>
            {
                UpdateCategories();

                if (ExpenseCategories.IsSelected(category))
                {
                    UpdateTransactions();
                }
            };

            categoryRepository.EntityAdded += (category) =>
            {
                UpdateCategories();
            };

            categoryRepository.EntityDeleted += (category) =>
            {
                UpdateCategories();
            };

            _transactionRepository.EntityAdded += (transaction) =>
            {
                UpdateCategories();
                UpdateTransactions();
                //Transactions.Add(transaction);
            };

            _transactionRepository.EntityUpdated += (transaction) =>
            {
                UpdateCategories();
                UpdateTransactions();
                //Transactions.AddOrUpdate(transaction, t => t.UID);
            };

            _transactionRepository.EntityDeleted += (transaction) =>
            {
                int index = Transactions.IndexOfFirst(t => t.UID.Equals(transaction.UID));

                if (index > -1)
                {
                    Transactions.RemoveAt(index);
                }

                UpdateCategories();
            };
        }

        private void UpdateTransactions()
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                Loading = true;
                try
                {
                    Category selectedCategory = ExpenseCategories.SelectedCategory?.Category;

                    // Get all transactions for selected category and filter
                    IEnumerable<Transaction> transactions = _transactionRepository.All(
                        filter: GetTransactionFilter(),
                        categories: selectedCategory);

                    CurrentBalance = _bankingService.GetBalance(EndDate, SelectedAccount);

                    Transactions.DynamicUpdate(
                        transactions,
                        (t1, t2) => t1.Idquals(t2),
                        new TransactionSortComparer(),
                        true);
                }
                finally
                {
                    Loading = false;
                }
            });
        }

        private TransactionFilter GetTransactionFilter()
        {
            return new TransactionFilter()
            {
                StartDate = FromDate,
                EndDate = EndDate,
                AccountNumber = _selectedAccount.Number
            };
        }

        private void UpdateCategories()
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                Loading = true;
                ExpenseCategories.UpdateCategories(GetTransactionFilter());
                Loading = false;
            });
        }

        private void HandleError(string message)
        {
            _statusMessageService.ShowMessage($"Error: {message}");
            //MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void OnSelect()
        {
            if (!_bankingService.HasBankingDetails)
            {
                // No bank connection configured -> show message?
                return;
            }

            Accounts = new(_bankingService.GetAccounts());

            if (Accounts.Any())
            {
                return;
            }
        }

        class TransactionSortComparer : IComparer<Transaction>
        {
            public int Compare(Transaction x, Transaction y)
            {
                return x.BookingDate.CompareTo(y.BookingDate);
            }
        }
    }
}