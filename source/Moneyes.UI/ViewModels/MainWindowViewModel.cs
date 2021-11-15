﻿using Moneyes.Data;
using Moneyes.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

                value.OnSelect();
                OnPropertyChanged();
                _currentViewModel = value;
            }
        }

        public MainWindowViewModel(IEnumerable<ITabViewModel> tabs, IBankConnectionStore bankingConfigStore,
            IStatusMessageService statusMessageService)
        {
            Tabs = new(tabs);

            if (!bankingConfigStore.HasBankingDetails)
            {
                CurrentViewModel = tabs.OfType<BankingSettingsViewModel>().FirstOrDefault();
            }
            else
            {
                CurrentViewModel = tabs.OfType<TransactionsViewModel>().FirstOrDefault();
            }

            statusMessageService.NewMessage += (msg, actText, act) => NewStatusMessage?.Invoke(msg, actText, act);

            //new View.CategoryView() { DataContext = new AddCategoryViewModel() }
        }

        public event Action<string, string?, Action?> NewStatusMessage;
    }
}
