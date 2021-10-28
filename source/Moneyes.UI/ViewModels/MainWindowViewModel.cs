using Moneyes.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.UI.ViewModels
{
    class MainWindowViewModel : ViewModelBase
    {
        public List<ITabViewModel> Tabs { get; set; }
        public ITabViewModel CurrentViewModel { get; set; }

        public MainWindowViewModel(IEnumerable<ITabViewModel> tabs, BankConnectionStore bankingConfigStore)
        {
            Tabs = new(tabs);

            if (!bankingConfigStore.HasBankingDetails)
            {
                CurrentViewModel = tabs.OfType<BankingSettingsViewModel>().FirstOrDefault();
            }
            else
            {
                CurrentViewModel = tabs.OfType<MainViewModel>().FirstOrDefault();
            }
        }
    }
}
