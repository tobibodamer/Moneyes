using Moneyes.Core;
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
    public class AccountsViewModel : TabViewModelBase
    {
        private readonly LiveDataService _liveDataService;
        private readonly IBankingService _bankingService;
        private readonly IStatusMessageService _statusMessageService;
        private readonly IDialogService<ImportAccountsViewModel> _importAccountsDialogService;

        private ObservableCollection<AccountDetails> _accounts = new();
        public ObservableCollection<AccountDetails> Accounts
        {
            get => _accounts;
            set
            {
                _accounts = value;

                //if (value != null && value.Any())
                //{
                //    SelectedAccount = value.First();
                //}

                OnPropertyChanged(nameof(Accounts));
            }
        }

        private BankDetails _selectedBankConnection;
        public BankDetails SelectedBankConnection
        {
            get => _selectedBankConnection;
            set
            {
                _selectedBankConnection = value;

                OnPropertyChanged();

                UpdateAccounts();
                ImportAccountsCommand.RaiseCanExecuteChanged();
            }
        }

        private ObservableCollection<BankDetails> _bankConnections = new();
        public ObservableCollection<BankDetails> BankConnections
        {
            get => _bankConnections;
            set
            {
                _bankConnections = value;

                OnPropertyChanged(nameof(BankConnections));
            }
        }

        public ICommand LoadedCommand { get; }

        public AsyncCommand ImportAccountsCommand { get; }

        //public bool HasBankConnection => _bankingService.GetBankEntries().Any();

        public AccountsViewModel(
            LiveDataService liveDataService,
            IBankingService bankingService,
            IStatusMessageService statusMessageService,
            IDialogService<ImportAccountsViewModel> importAccountsDialogService)
        {
            DisplayName = "Accounts";

            _liveDataService = liveDataService;
            _bankingService = bankingService;
            _statusMessageService = statusMessageService;
            _importAccountsDialogService = importAccountsDialogService;

            ImportAccountsCommand = new AsyncCommand(async ct =>
            {
                var accountResult = await _liveDataService.FetchAccounts(SelectedBankConnection);

                if (accountResult.IsSuccessful)
                {
                    IEnumerable<AccountDetails> accounts = accountResult.Data;

                    ImportAccountsViewModel importAccountsViewModel = new(accounts);

                    DialogResult dialogResult = _importAccountsDialogService
                        .ShowDialog(importAccountsViewModel);

                    if (dialogResult == DialogResult.OK)
                    {
                        IEnumerable<AccountDetails> selectedAccounts = importAccountsViewModel.SelectedAccounts;
                        int numAccountsImported = _bankingService.ImportAccounts(selectedAccounts);

                        _statusMessageService.ShowMessage($"{numAccountsImported} new accounts imported.");

                        Accounts = new(_bankingService.GetAccounts(SelectedBankConnection));
                    }
                }
                else
                {
                    _statusMessageService.ShowMessage("Failed importing accounts.", "Retry",
                        () => ImportAccountsCommand.Execute(null));
                }

            },
            () => SelectedBankConnection != null,
            errorHandler: (ex) => _statusMessageService.ShowMessage("Error while importing accounts"));
        }

        public void UpdateBankConnections()
        {
            var bankConnections = _bankingService.GetBankEntries();

            var selectedTemp = SelectedBankConnection?.Id;

            BankConnections.DynamicUpdate(bankConnections, (b1, b2) => b1.Id == b2.Id);

            if (selectedTemp != null)
            {
                SelectedBankConnection = BankConnections.FirstOrDefault(x => x.Id == selectedTemp);
            }
            else
            {
                SelectedBankConnection = bankConnections.FirstOrDefault();
            }

        }
        public void UpdateAccounts()
        {
            if (SelectedBankConnection == null)
            {
                return;
            }

            var accounts = _bankingService.GetAccounts(SelectedBankConnection);

            Accounts.DynamicUpdate(accounts, (a1, a2) => a1.Id == a2.Id);
        }

        public override void OnSelect()
        {
            base.OnSelect();

            UpdateBankConnections();
        }
    }
}
