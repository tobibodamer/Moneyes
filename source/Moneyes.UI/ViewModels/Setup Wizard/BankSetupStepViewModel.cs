using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    class BankSetupStepViewModel : BankSetupViewModel, IWizardStepViewModel
    {
        public BankSetupStepViewModel(ILiveDataService liveDataService, IBankingService bankingService)
            : base(liveDataService, bankingService)
        {
            PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(State) && State == BankSetupState.ConnectionSuccessful)
                {
                    TransitionController?.NextStepCommand.Execute(null);
                }
            };
        }

        public ICommand CancelCommand => TransitionController?.FinishCommand;

        public ITransitionController TransitionController { get; set; }

        public Task OnTransitedFrom(TransitionContext transitionContext)
        {
            return Task.CompletedTask;
        }

        public Task OnTransitedTo(TransitionContext transitionContext)
        {
            OnSelect();

            return Task.CompletedTask;
        }
    }
}
