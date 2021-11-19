using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    public interface ITransitionController
    {
        public ICommand NextStepCommand { get; }
        public ICommand PreviousStepCommand { get; }
        public ICommand FinishCommand { get; }
    }
}
