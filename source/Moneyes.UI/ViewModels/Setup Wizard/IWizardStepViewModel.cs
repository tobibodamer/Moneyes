using System;
using System.Threading.Tasks;

namespace Moneyes.UI.ViewModels
{
    public interface IWizardStepViewModel
    {
        ITransitionController TransitionController { get; set; }
        Task OnTransitedTo(TransitionContext transitionContext);
        Task OnTransitedFrom(TransitionContext transitionContext);
    }
}
