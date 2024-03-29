﻿using Moneyes.Core;
using Moneyes.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    internal class FinishedStepViewModel : ViewModelBase, IWizardStepViewModel
    {
        private readonly LiveDataService _liveDataService;
        private readonly IBankingService _bankingService;
        public ICommand FinishCommand => TransitionController.NextStepCommand;
        public ITransitionController TransitionController { get; set; }

        private bool _fetchAfterFinish = true;
        public bool FetchAfterFinish
        {
            get => _fetchAfterFinish;
            set
            {
                _fetchAfterFinish = value;
                OnPropertyChanged();
            }
        }

        public FinishedStepViewModel(
            LiveDataService liveDataService,
            IBankingService bankingService,
            IStatusMessageService statusMessageService)
        {
            _liveDataService = liveDataService;
            _bankingService = bankingService;
        }

        private BankDetails _bankDetails;

        public async Task OnTransitedTo(TransitionContext transitionContext)
        {
            if (transitionContext.Argument is BankDetails bankDetails)
            {
                _bankDetails = bankDetails;
            }

            await Task.CompletedTask;
        }

        public async Task OnTransitedFrom(TransitionContext transitionContext)
        {
            if (!FetchAfterFinish || _bankDetails == null)
            {
                return;
            }

            var accounts = _bankingService.GetAccounts(_bankDetails);

            if (accounts.Any())
            {
                _ = await _liveDataService.FetchTransactionsAndBalances(accounts.ToArray());
            }
        }
    }
}