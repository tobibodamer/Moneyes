using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace Moneyes.UI.ViewModels
{
    internal class TransactionsViewModel : ViewModelBase
    {
        private readonly TransactionRepository _transactionRepository;

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

        public TransactionsViewModel(TransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public void UpdateTransactions(TransactionFilter filter, params Category[] categories)
        {
            _ = Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                IsLoading = true;
                try
                {
                    // Get all transactions for selected category and filter
                    var transactions = _transactionRepository.All(
                            filter: filter,
                            categories: categories)
                        .ToList();

                    Transactions.DynamicUpdate(
                        transactions,
                        (t1, t2) => t1.Idquals(t2),
                        new TransactionSortComparer(),
                        true);
                }
                finally
                {
                    IsLoading = false;
                }
            });
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
