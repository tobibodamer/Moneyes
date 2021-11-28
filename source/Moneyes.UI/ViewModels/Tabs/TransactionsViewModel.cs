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
        private readonly ICategoryService _categoryService;
        private Balance _currentBalance;
        private bool _isLoaded = false;

        private ObservableCollection<Transaction> _transactions = new();

        #region UI Properties
        public ICommand LoadedCommand { get; }

        public ICommand RemoveFromCategory { get; }

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


        private ObservableCollection<Transaction> _selectedTransactions;
        public ObservableCollection<Transaction> SelectedTransactions
        {
            get => _selectedTransactions;
            set
            {
                _selectedTransactions = value;
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
            CategoryRepository categoryRepository,
            ICategoryService categoryService)
        {
            DisplayName = "Transactions";
            Categories = expenseCategoriesViewModel;
            Selector = selectorViewModel;

            _categoryService = categoryService;
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

            categoryRepository.RepositoryChanged += (args) =>
            {
                UpdateCategories();

                if (args.Actions.HasFlag(RepositoryChangedAction.Replace)
                 && args.ReplacedItems.Any(c => Categories.IsSelected(c)))
                {
                    UpdateTransactions();
                }
            };

            transactionRepository.RepositoryChanged += (args) =>
            {
                UpdateCategories();
                UpdateTransactions();
            };

            Selector.SelectorChanged += (sender, args) =>
            {
                UpdateCategories();
                UpdateTransactions();
            };

            RemoveFromCategory = new CollectionRelayCommand<Transaction>(transactions =>
            {
                Category selectedCategory = Categories.SelectedCategory.Category;

                foreach (Transaction t in transactions)
                {
                    if (t.Categories.Contains(selectedCategory))
                    {
                        _categoryService.RemoveFromCategory(t, selectedCategory);
                    }
                }
            }, transactions =>
            {
                return transactions != null
                    && transactions.All(transaction => transaction != null &&
                        Categories.SelectedCategory != null &&
                        Categories.SelectedCategory.IsRealCategory &&
                        transaction.Categories.Contains(Categories.SelectedCategory.Category));
            });
        }

        private void UpdateTransactions()
        {
            _ = Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                Loading = true;
                try
                {
                    Category selectedCategory = Categories.SelectedCategory?.Category;

                    var withSubCategories = _categoryService.GetSubCategories(selectedCategory)
                        .Concat(new Category[] { selectedCategory });

                    // Get all transactions for selected category and filter
                    var transactions = _transactionRepository.All(
                            filter: GetTransactionFilter(),
                            categories: selectedCategory)
                        .ToList();

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