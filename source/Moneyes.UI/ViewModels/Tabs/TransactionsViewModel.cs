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
        private bool _isLoaded = false;

        private ObservableCollection<Transaction> _transactions = new();

        #region UI Properties
        public ICommand LoadedCommand { get; }

        public ExpenseCategoriesViewModel Categories { get; }

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
            Categories = expenseCategoriesViewModel;
            Selector = selectorViewModel;
            _liveDataService = liveDataService;
            _transactionRepository = transactionRepository;
            _bankingService = bankingService;
            _statusMessageService = statusMessageService;


            Categories.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(Categories.SelectedCategory))
                {
                    UpdateTransactions();
                }
            };

            categoryRepository.EntityUpdated += (category) =>
            {
                Debug.WriteLine("Category updated");
                UpdateCategories();

                if (Categories.IsSelected(category))
                {
                    UpdateTransactions();
                }
            };

            categoryRepository.EntityAdded += (category) =>
            {
                Debug.WriteLine("Category added");
                UpdateCategories();
            };

            categoryRepository.EntityDeleted += (category) =>
            {
                Debug.WriteLine("Category deleted");
                UpdateCategories();
            };

            _transactionRepository.EntityAdded += (transaction) =>
            {
                Debug.WriteLine("Entity added");
                UpdateCategories();
                UpdateTransactions();
                //Transactions.Add(transaction);
            };

            _transactionRepository.EntityUpdated += (transaction) =>
            {
                Debug.WriteLine("Entity updated");
                UpdateCategories();
                UpdateTransactions();
                //Transactions.AddOrUpdate(transaction, t => t.UID);
            };

            _transactionRepository.EntityDeleted += (transaction) =>
            {
                Debug.WriteLine("Entity deleted");
                int index = Transactions.IndexOfFirst(t => t.UID.Equals(transaction.UID));

                if (index > -1)
                {
                    Transactions.RemoveAt(index);
                }

                UpdateCategories();
            };

            Selector.SelectorChanged += (sender, args) =>
            {
                //Debug.WriteLine("Selector changed");
                UpdateCategories();
                UpdateTransactions();
            };
        }

        private void UpdateTransactions()
        {
            _ = Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                Loading = true;
                try
                {
                    Category selectedCategory = Categories.SelectedCategory?.Category;


                    // Get all transactions for selected category and filter
                    IEnumerable<Transaction> transactions = _transactionRepository.All(
                        filter: GetTransactionFilter(),
                        categories: selectedCategory);

                    Transactions.DynamicUpdate(
                        transactions,
                        (t1, t2) => t1.Idquals(t2),
                        new TransactionSortComparer(),
                        true);

                    if (Selector.CurrentAccount != null)
                    {
                        CurrentBalance = _bankingService.GetBalance(Selector.EndDate, Selector.CurrentAccount);
                    }
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
            _ = Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                Categories.UpdateCategories(GetTransactionFilter());
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

            if (!_isLoaded)
            {
                UpdateCategories();
                UpdateTransactions();

                _isLoaded = true;
            }
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