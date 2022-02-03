using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using Moneyes.UI.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace Moneyes.UI.ViewModels
{
    internal class TransactionsViewModel : ViewModelBase
    {
        private readonly ITransactionService _transactionService;
        private readonly ICategoryService _categoryService;

        private ObservableCollection<Transaction> _transactions = new();
        public ObservableCollection<Transaction> Transactions
        {
            get
            {
                return _transactions;
            }
            set
            {
                _transactions = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<Category> Categories => _categoryService.GetCategories(CategoryTypes.Real);

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public ICommand RemoveFromCategory { get; set; }

        public ICommand AddToCategory { get; set; }

        public ICommand AddToNewCategory { get; set; }

        public ObservableCollection<Transaction> SelectedTransactions { get; set; } = new();

        public TransactionsViewModel(ITransactionService transactionService, ICategoryService categoryService,
            IStatusMessageService statusMessageService)
        {
            _transactionService = transactionService;
            _categoryService = categoryService;

            CategoryViewModelFactory categoryViewModelFactory = new(categoryService, statusMessageService);

            RemoveFromCategory = new CollectionRelayCommand<Transaction>(transactions =>
            {
                foreach (Transaction t in transactions)
                {
                    categoryService.RemoveFromCategory(t);
                }
            }, transactions =>
            {
                return transactions != null && transactions.Any(t => t.Category != null);
            });

            AddToCategory = new RelayCommand<Category>(category =>
            {
                var transactions = SelectedTransactions;

                foreach (Transaction t in transactions)
                {
                    categoryService.MoveToCategory(t, category);
                }

                statusMessageService.ShowMessage($"Added to category \"{category.Name}\".");
            });

            AddToNewCategory = new RelayCommand(() =>
            {
                var transactions = SelectedTransactions;

                AddCategoryViewModel = categoryViewModelFactory.CreateAddCategoryViewModel();
                AddCategoryViewModel.Saved += (category) =>
                {

                    foreach (var t in transactions)
                    {
                        categoryService.MoveToCategory(t, category);
                    }
                };
            });

        }

        private EditCategoryViewModel _addCategoryViewModel;
        public EditCategoryViewModel AddCategoryViewModel
        {
            get
            {
                return _addCategoryViewModel;
            }
            set
            {
                _addCategoryViewModel = value;
                OnPropertyChanged();
            }
        }


        private CancellationTokenSource _updateCTS = new();
        public async Task UpdateTransactions(TransactionFilter filter, params Category[] categories)
        {
            if (IsLoading)
            {
                _updateCTS.Cancel();

                while (IsLoading)
                {
                    await Task.Delay(5);
                }
            }

            _updateCTS = new();

            IsLoading = true;
            try
            {
                List<Transaction> transactions =
                    await Task.Run(() =>
                    {
                        return _transactionService.All(
                            filter: filter,
                            categories: categories)
                        .ToList();
                    });

                if (_updateCTS.IsCancellationRequested)
                {
                    return;
                }

                Transactions.DynamicUpdate(
                       transactions,
                       (t1, t2) => t1.Id.Equals(t2.Id),
                       new TransactionSortComparer(),
                       true);
            }
            finally
            {
                IsLoading = false;

                _updateCTS.Dispose();
                _updateCTS = null;
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
