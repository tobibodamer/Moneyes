using Moneyes.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.UI
{
    class NavigationStore
    {
        public event Action CurrentViewModelChanged;

        private ViewModelBase _currentViewModel;

        public ViewModelBase CurrentViewModel {
            get { return _currentViewModel; }
            set
            {
                if (_currentViewModel == value) { return; }

                _currentViewModel = value;
                CurrentViewModelChanged?.Invoke();
            }
        }
    }
}
