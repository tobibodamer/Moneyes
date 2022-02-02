using Moneyes.Core;
using Moneyes.LiveData;
using Moneyes.UI.Services;
using Moneyes.UI.View;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    class SelectorViewModel : ViewModelBase
    {
        private IBankingService _bankingService;
        private LiveDataService _liveDataService;
        private SelectorStore _selectorStore;

        private ObservableCollection<AccountDetails> _accounts = new();
        public ObservableCollection<AccountDetails> Accounts
        {
            get => _accounts;
            set
            {
                _accounts = value;

                OnPropertyChanged();
            }
        }

        public AccountDetails CurrentAccount
        {
            get => _selectorStore.CurrentAccount;
            set
            {
                _selectorStore.CurrentAccount = value;

                OnPropertyChanged();
            }
        }
        public DateTime FromDate
        {
            get => _selectorStore.StartDate;
            set
            {
                _selectorStore.StartDate = value;
                OnPropertyChanged();
            }
        }
        public DateTime EndDate
        {
            get => _selectorStore.EndDate;
            set
            {
                _selectorStore.EndDate = value;
                OnPropertyChanged();
            }
        }

        private DateSelectionMode _dateSelection = DateSelectionMode.Month;
        public DateSelectionMode DateSelection
        {
            get => _dateSelection;
            set
            {
                _dateSelection = value;
                OnPropertyChanged();
            }
        }

        public ICommand ApplyDateCommand { get; }

        public AsyncCommand FetchOnlineCommand { get; }

        public SelectorViewModel(
            IBankingService bankingService, LiveDataService liveDataService, SelectorStore selectorStore,
            IStatusMessageService statusMessageService)
        {
            _bankingService = bankingService;
            _liveDataService = liveDataService;
            _selectorStore = selectorStore;

            FromDate = new(DateTime.Now.Year, DateTime.Now.Month, 1);
            EndDate = DateTime.Now;

            FetchOnlineCommand = new AsyncCommand(async ct =>
            {
                Result<int> result = await _liveDataService.FetchTransactionsAndBalances(CurrentAccount);

                if (result.IsSuccessful)
                {
                    if (result.Data == 0)
                    {
                        statusMessageService.ShowMessage($"No new transactions available");
                        return;
                    }

                    statusMessageService.ShowMessage($"Fetched {result.Data} new transaction(s)");
                }
                else
                {
                    statusMessageService.ShowMessage($"Fetching transactions failed", "Retry",
                        () => FetchOnlineCommand.Execute(null));
                }
            }, () => CurrentAccount != null);

            ApplyDateCommand = new RelayCommand(() =>
            {
                OnPropertyChanged(nameof(FromDate));
                OnPropertyChanged(nameof(EndDate));
                SelectorChanged?.Invoke(this, EventArgs.Empty);
            });

            _selectorStore.AccountChanged += SelectorStore_AccountChanged;

            RefreshAccounts();

            if (Accounts.Any())
            {
                if (!Accounts.Contains(CurrentAccount))
                {
                    CurrentAccount = Accounts.First();
                }
                return;
            }

            bankingService.NewAccountsImported += BankingService_NewAccountsImported;
        }

        private void BankingService_NewAccountsImported()
        {
            RefreshAccounts();

            if (CurrentAccount == null)
            {
                CurrentAccount = Accounts.First();
            }
        }

        ~SelectorViewModel()
        {
            _selectorStore.AccountChanged -= SelectorStore_AccountChanged;
            _bankingService.NewAccountsImported -= BankingService_NewAccountsImported;
        }

        private void SelectorStore_AccountChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(CurrentAccount));
            SelectorChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RefreshAccounts()
        {
            Accounts = new(_bankingService.GetAllAccounts());

            FetchOnlineCommand.RaiseCanExecuteChanged();
        }

        public event EventHandler SelectorChanged;
    }
}