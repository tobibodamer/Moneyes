using Moneyes.Data;
using Moneyes.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public List<ITabViewModel> Tabs { get; set; }

        private ITabViewModel _currentViewModel;
        public ITabViewModel CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                if (_currentViewModel == value)
                {
                    return;
                }

                _currentViewModel?.OnDeselect();
                _currentViewModel = value;
                value?.OnSelect();

                OnPropertyChanged();
            }
        }

        private SetupWizardViewModel _setupWizard;
        public SetupWizardViewModel SetupWizard
        {
            get => _setupWizard;
            set
            {
                _setupWizard = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoadedCommand { get; }

        public MainWindowViewModel(IEnumerable<ITabViewModel> tabs, IBankingService bankingService,
            IStatusMessageService statusMessageService, SetupWizardViewModel setupWizardViewModel)
        {
            Tabs = new(tabs);

            LoadedCommand = new RelayCommand(() =>
            {
                if (!bankingService.HasBankingDetails)
                {
                    SetupWizard = setupWizardViewModel;
                }
            });

            CurrentViewModel = tabs.OfType<TransactionsTabViewModel>().FirstOrDefault();

            statusMessageService.NewMessage += (msg, actText, act) => NewStatusMessage?.Invoke(msg, actText, act);
        }

        public event Action<string, string, Action> NewStatusMessage;
    }
}
