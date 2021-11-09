﻿using Moneyes.Core;
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
    class AccountsViewModel : ViewModelBase, ITabViewModel
    {
        private readonly LiveDataService _liveDataService;
        private readonly IBankingService _bankingService;
        private readonly IStatusMessageService _statusMessageService;
        private readonly IDialogService<ImportAccountsViewModel> _importAccountsDialogService;

        private ObservableCollection<AccountDetails> _accounts;
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

        public ICommand LoadedCommand { get; }

        public ICommand ImportAccountsCommand { get; }

        public bool HasBankConnection => _bankingService.HasBankingDetails;

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

            LoadedCommand = new AsyncCommand(async ct =>
            {
                
            });

            ImportAccountsCommand = new AsyncCommand(async ct =>
            {
                var accountResult = await _liveDataService.FetchAccounts();

                if (accountResult.IsSuccessful)
                {
                    IEnumerable<AccountDetails> accounts = accountResult.Data;

                    ImportAccountsViewModel importAccountsViewModel = new(accounts);
                    DialogResult dialogResult = _importAccountsDialogService
                    .ShowDialog(importAccountsViewModel);

                    if (dialogResult == DialogResult.OK)
                    {
                        IEnumerable<AccountDetails> selectedAccounts = importAccountsViewModel.SelectedAccounts;
                        int numAccountsImported = _bankingService.ImportAccounts(accounts);

                        _statusMessageService.ShowMessage($"{numAccountsImported} new accounts imported.");

                        Accounts = new(_bankingService.GetAccounts());
                    }
                }
                else
                {
                    _statusMessageService.ShowMessage("Failed importing accounts.", "Retry",
                        () => ImportAccountsCommand.Execute(null));
                }

            },
            () => _bankingService.HasBankingDetails,
            errorHandler: (ex) => _statusMessageService.ShowMessage("Error while importing accounts"));
        }

        public void OnSelect()
        {
            if (!_bankingService.HasBankingDetails)
            {
                // No bank connection configured -> show message?
                return;
            }

            Accounts = new(_bankingService.GetAccounts());
        }
    }
}
