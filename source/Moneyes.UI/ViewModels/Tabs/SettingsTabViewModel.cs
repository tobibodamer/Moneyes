using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.UI.ViewModels
{
    public class SettingsTabViewModel : TabViewModelBase
    {
        public BankContactsViewModel BankContactSettings
        {
            get; set;
        }

        public SettingsTabViewModel(IBankingService bankingService)
        {
            DisplayName = "Settings";
            BankContactSettings = new(bankingService);
        }

        public override void OnSelect()
        {
            base.OnSelect();
            BankContactSettings.UpdateBankConnections();
        }
    }
}
