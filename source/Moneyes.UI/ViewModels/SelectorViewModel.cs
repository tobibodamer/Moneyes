using Moneyes.Core;
using Moneyes.UI.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

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

                //if (value != null && value.Any())
                //{
                //    if (CurrentAccount == null)
                //    {
                //        CurrentAccount = value.First();
                //    }                    
                //}

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

        public AsyncCommand MonthMinusCommand { get; }
        public AsyncCommand MonthPlusCommand { get; }

        public SelectorViewModel(
            IBankingService bankingService, LiveDataService liveDataService, SelectorStore selectorStore)
        {
            _bankingService = bankingService;
            _liveDataService = liveDataService;
            _selectorStore = selectorStore;

            FromDate = new(DateTime.Now.Year, DateTime.Now.Month, 1); ;
            EndDate = DateTime.Now;

            MonthMinusCommand = new AsyncCommand(async ct =>
            {
                FromDate = FromDate.AddMonths(-1);

                var lastDayOfMonth = FromDate.Month == DateTime.Now.Month
                    ? DateTime.Now.Day : DateTime.DaysInMonth(FromDate.Year, FromDate.Month);

                EndDate = new(FromDate.Year, FromDate.Month, lastDayOfMonth);

                MonthPlusCommand.RaiseCanExecuteChanged();
            });

            MonthPlusCommand = new AsyncCommand(async ct =>
            {
                FromDate = FromDate.AddMonths(1);

                var lastDayOfMonth = FromDate.Month == DateTime.Now.Month
                    ? DateTime.Now.Day : DateTime.DaysInMonth(FromDate.Year, FromDate.Month);

                EndDate = new(FromDate.Year, FromDate.Month, lastDayOfMonth);
            }, () =>
            {
                var nextMonth = FromDate.AddMonths(1);

                return nextMonth.Year < DateTime.Now.Year
                    || (nextMonth.Year == DateTime.Now.Year && nextMonth.Month <= DateTime.Now.Month);
            });


            _selectorStore.AccountChanged += SelectorStore_AccountChanged;
            _selectorStore.DateChanged += SelectorStore_DateChanged;

            RefreshAccounts();

            if (Accounts.Any())
            {
                if (!Accounts.Contains(CurrentAccount))
                {
                    CurrentAccount = Accounts.First();
                }
                return;
            }
        }

        ~SelectorViewModel()
        {
            _selectorStore.AccountChanged -= SelectorStore_AccountChanged;
            _selectorStore.DateChanged -= SelectorStore_DateChanged;
        }

        private void SelectorStore_DateChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(FromDate));
            OnPropertyChanged(nameof(EndDate));
            SelectorChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SelectorStore_AccountChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(CurrentAccount));
            SelectorChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RefreshAccounts()
        {
            if (!_bankingService.HasBankingDetails)
            {
                // No bank connection configured -> show message?
                return;
            }

            Accounts = new(_bankingService.GetAccounts());
        }

        public event EventHandler SelectorChanged;
    }
}