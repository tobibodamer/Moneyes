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
        private Balance _currentBalance;

        private ObservableCollection<Transaction> _transactions = new();

        #region UI Properties
        public ICommand LoadedCommand { get; }
        public ICommand FetchOnlineCommand { get; }
        public ICommand SelectCategoryCommand { get; }


        public ExpenseCategoriesViewModel ExpenseCategories { get; }

        private SelectorViewModel _selector;
        public SelectorViewModel Selector
        {
            get => _selector;
            set
            {
                _selector = value;
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

        #endregion
        public TransactionsViewModel(
            LiveDataService liveDataService,
            TransactionRepository transactionRepository,
            IBankingService bankingService,
            IStatusMessageService statusMessageService,
            ExpenseCategoriesViewModel expenseCategoriesViewModel,
            SelectorViewModel selectorViewModel,
            CategoryRepository categoryRepository)
        {
            DisplayName = "Transactions";
            ExpenseCategories = expenseCategoriesViewModel;
            Selector = selectorViewModel;
            _liveDataService = liveDataService;
            _transactionRepository = transactionRepository;
            _bankingService = bankingService;
            _statusMessageService = statusMessageService;

            FetchOnlineCommand = new AsyncCommand(async ct =>
            {
                Result<int> result = await _liveDataService
                    .FetchTransactionsAndBalances(Selector.CurrentAccount);

                if (result.IsSuccessful)
                {
                    if (result.Data == 0)
                    {
                        _statusMessageService.ShowMessage($"No new transactions available");
                        return;
                    }

                    //UpdateCategories();
                    //UpdateTransactions();

                    _statusMessageService.ShowMessage($"Fetched {result.Data} new transaction(s)");
                }
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

            Selector.SelectorChanged += (sender, args) =>
            {
                UpdateCategories();
                UpdateTransactions();
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

                    if (Selector.CurrentAccount != null)
                    {
                        CurrentBalance = _bankingService.GetBalance(Selector.EndDate, Selector.CurrentAccount);
                    }

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
                StartDate = Selector.FromDate,
                EndDate = Selector.EndDate,
                AccountNumber = Selector.CurrentAccount?.Number
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
            Selector.RefreshAccounts();
            UpdateCategories();
            UpdateTransactions();
        }

        class TransactionSortComparer : IComparer<Transaction>
        {
            public int Compare(Transaction x, Transaction y)
            {
                return x.BookingDate.CompareTo(y.BookingDate) * -1;
            }
        }
    }
}