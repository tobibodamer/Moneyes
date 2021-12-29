using Moneyes.Core;
using Moneyes.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    internal class ImportAccountsStepViewModel : ImportAccountsViewModel, IWizardStepViewModel
    {
        private readonly LiveDataService _liveDataService;
        private readonly IBankingService _bankingService;

        public ICommand ImportCommand => TransitionController?.NextStepCommand;
        public ICommand CancelCommand => TransitionController?.FinishCommand;
        public ITransitionController TransitionController { get; set; }

        public ImportAccountsStepViewModel(
            LiveDataService liveDataService, IBankingService bankingService, 
            IStatusMessageService statusMessageService)
            : base(Enumerable.Empty<AccountDetails>())
        {
            _liveDataService = liveDataService;
            _bankingService = bankingService;
        }

        public async Task OnTransitedTo(TransitionContext transitionContext)
        {
            var accountResult = await _liveDataService.FetchAccounts();

            if (accountResult.IsSuccessful)
            {
                IEnumerable<AccountDetails> accounts = accountResult.Data;

                Accounts = new(accounts.Select(acc =>
                new AccountViewModel()
                {
                    Account = acc,
                    IsSelected = true
                }));
            }
        }

        public Task OnTransitedFrom(TransitionContext transitionContext)
        {
            int numAccountsImported = _bankingService.ImportAccounts(SelectedAccounts);

            //statusMessageService.ShowMessage($"{numAccountsImported} new accounts imported.");

            return Task.CompletedTask;
        }
    }
}
