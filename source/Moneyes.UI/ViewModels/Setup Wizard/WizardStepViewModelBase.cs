using System.Threading.Tasks;

namespace Moneyes.UI.ViewModels
{
    public abstract class WizardStepViewModelBase : ViewModelBase, IWizardStepViewModel
    {
        /// <inheritdoc />
        public ITransitionController TransitionController { get; set; }

        /// <inheritdoc />
        public virtual Task OnTransitedTo(TransitionContext transitionContext)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public virtual Task OnTransitedFrom(TransitionContext transitionContext)
        {
            return Task.CompletedTask;
        }
    }
}
