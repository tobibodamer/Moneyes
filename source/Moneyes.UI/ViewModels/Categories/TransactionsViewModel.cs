using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
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
                        return _transactionRepository.All(
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
                       (t1, t2) => t1.Idquals(t2),
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
