using Moneyes.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Moneyes.UI.Services
{
    interface IDialogService
    {
        DialogResult ShowDialog(ViewModelBase viewModel);
    }

    interface IDialogService<TViewModel> : IDialogService
        where TViewModel : ViewModelBase
    {
        DialogResult ShowDialog(TViewModel viewModel);
    }

    class DialogService<TDialog, TViewModel> : DialogService<TDialog>, IDialogService<TViewModel>
        where TViewModel : ViewModelBase
        where TDialog : Window, new()
    {
        public DialogResult ShowDialog(TViewModel viewModel)
        {
            return base.ShowDialog(viewModel);
        }
    }
    class DialogService<TDialog> : IDialogService
        where TDialog : Window, new()
    {
        public DialogResult ShowDialog(ViewModelBase viewModel)
        {
            TDialog dialog = new()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() ?? false)
            {
                return DialogResult.OK;
            }

            return DialogResult.Cancel;
        }
    }
}
