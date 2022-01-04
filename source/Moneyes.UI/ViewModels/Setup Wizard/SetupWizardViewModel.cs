using Moneyes.UI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    public class TransitionContext
    {
        /// <summary>
        /// Gets the index of the step is being navigated away from.
        /// </summary>
        public int TransitedFromStep { get; internal set; }

        /// <summary>
        /// Gets or sets index of the step has been navigated to.
        /// </summary>
        public int TransitToStep { get; set; }

        /// <summary>
        /// Gets whether this transition was caused by invoking the <see cref="ITransitionController.FinishCommand"/>.
        /// </summary>
        public bool IsFinishTransition { get; init; }
    }
    public class SetupWizardViewModel : ViewModelBase, IDialogViewModel, ITransitionController
    {
        private IWizardStepViewModel _currentStep;
        public IWizardStepViewModel CurrentStep
        {
            get => _currentStep;
            set
            {
                _currentStep = value;
                OnPropertyChanged();
            }
        }

        private bool _isTransiting;
        public bool IsTransiting
        {
            get => _isTransiting;
            set
            {
                _isTransiting = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<IWizardStepViewModel> Steps { get; }

        public AsyncCommand ApplyCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand NextStepCommand { get; }
        public ICommand PreviousStepCommand { get; }
        public AsyncCommand FinishCommand { get; }

        ICommand IDialogViewModel.ApplyCommand => ApplyCommand;
        ICommand ITransitionController.FinishCommand => FinishCommand;

        public event EventHandler<RequestCloseDialogEventArgs> RequestClose;

        public SetupWizardViewModel(ILiveDataService liveDataService,
            IBankingService bankingService, IStatusMessageService statusMessageService)
        {
            Steps = new();
            Steps.CollectionChanged += Steps_CollectionChanged;

            BankSetupStepViewModel bankSetup = new(liveDataService, bankingService);

            ImportAccountsStepViewModel accountImport = new(
                    liveDataService, bankingService, statusMessageService);

            FinishedStepViewModel finishedStep = new(
                        liveDataService, bankingService, statusMessageService);

            Steps.Add(bankSetup);
            Steps.Add(accountImport);
            Steps.Add(finishedStep);

            ApplyCommand = new AsyncCommand(async ct =>
            {
                RequestClose?.Invoke(this, new() { Result = true });
            });

            NextStepCommand = new AsyncCommand(async ct =>
            {
                await NextStep();
            });

            PreviousStepCommand = new AsyncCommand(async ct =>
            {
                await PreviousStep();
            });

            FinishCommand = new AsyncCommand(async ct =>
            {
                await ApplyCommand.ExecuteAsync();
            });

            NextStep().FireAndForgetSafeAsync();
        }

        private void Steps_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var step in e.NewItems.Cast<IWizardStepViewModel>())
                {
                    //step.Finished += StepViewModel_Finished;
                    step.TransitionController = this;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var step in e.OldItems.Cast<IWizardStepViewModel>())
                {
                    //step.Finished -= StepViewModel_Finished;
                    step.TransitionController = null;
                }
            }
        }

        private async Task NextStep()
        {
            int nextIndex = Steps.IndexOf(CurrentStep) + 1;

            await TryTransit(nextIndex);
        }

        private async Task PreviousStep()
        {
            int nextIndex = Steps.IndexOf(CurrentStep) - 1;

            await TryTransit(nextIndex);
        }

        /// <summary>
        /// Try transiting to the next step. If the next step is out of index, execute finish command.
        /// </summary>
        /// <param name="transitToIndex"></param>
        /// <returns></returns>
        private async Task TryTransit(int transitToIndex)
        {
            try
            {
                IsTransiting = true;

                int transitFromIndex = Steps.IndexOf(CurrentStep);
                bool isFinishTransit = transitToIndex > Steps.Count - 1;

                if (transitToIndex < 0)
                {
                    return;
                }

                TransitionContext transitionContext = new()
                {
                    TransitToStep = transitToIndex,
                    TransitedFromStep = transitFromIndex,
                    IsFinishTransition = isFinishTransit
                };

                if (CurrentStep != null)
                {
                    await CurrentStep.OnTransitedFrom(transitionContext);
                }

                if (!isFinishTransit)
                {
                    IWizardStepViewModel nextStep = Steps[transitToIndex];

                    await nextStep.OnTransitedTo(transitionContext);

                    CurrentStep = nextStep;

                    return;
                }

                CurrentStep = null;
                await FinishCommand.ExecuteAsync();
            }
            finally
            {
                IsTransiting = false;
            }
        }
    }
}
