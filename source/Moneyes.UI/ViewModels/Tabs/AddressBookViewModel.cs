using Moneyes.Core;
using Moneyes.Data;
using Moneyes.LiveData;
using Moneyes.UI.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    public class AddressBookViewModel : TabViewModelBase
    {
        private readonly LiveDataService _liveDataService;
        private readonly IBankingService _bankingService;
        private readonly IStatusMessageService _statusMessageService;

        private ObservableCollection<string> _accounts;
        public ObservableCollection<string> Accounts
        {
            get => _accounts;
            set
            {
                _accounts = value;

                OnPropertyChanged(nameof(Accounts));
            }
        }

        private string _selectedAccount;
        public string SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                _selectedAccount = value;
                OnPropertyChanged();
                OnSelectedAccountChanged();
            }
        }

        private ObservableCollection<Transaction> _transactions;
        public ObservableCollection<Transaction> Transactions
        {
            get => _transactions;
            set
            {
                _transactions = value;

                OnPropertyChanged();
            }
        }

        private Dictionary<string, List<Transaction>> _accountTransactionsMap;

        public AddressBookViewModel(
            LiveDataService liveDataService,
            IBankingService bankingService,
            IStatusMessageService statusMessageService,
            TransactionRepository transactionRepository)
        {
            DisplayName = "Account Book";

            _liveDataService = liveDataService;
            _bankingService = bankingService;
            _statusMessageService = statusMessageService;


            var groupedTransactions = transactionRepository
                .GetAll()
                .ToList()
                .GroupBy(t => t.Name)
                .OrderBy(g => g.Key);

            _accountTransactionsMap = groupedTransactions.ToDictionary(g => g.Key ?? "", g => g.ToList());

            Accounts = new(groupedTransactions.Select(g => g.Key));
        }

        public override void OnSelect()
        {
            base.OnSelect();
        }

        private void OnSelectedAccountChanged()
        {
            var key = string.IsNullOrEmpty(SelectedAccount) ? "" : SelectedAccount;
            Transactions = new(_accountTransactionsMap.GetValueOrDefault(key));
        }
    }
}
