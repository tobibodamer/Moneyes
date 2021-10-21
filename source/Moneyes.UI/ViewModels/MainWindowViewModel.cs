using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.UI.ViewModels
{
    class MainWindowViewModel : ViewModelBase
    {
        public List<ViewModelBase> Tabs { get; set; }
        public ViewModelBase CurrentViewModel { get; set; }
    }
}
