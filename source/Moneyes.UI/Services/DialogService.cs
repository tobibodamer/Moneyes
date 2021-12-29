using Moneyes.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Moneyes.UI.Services
{
    public interface IDialogService
    {
        DialogResult ShowDialog(ViewModelBase viewModel);
        Task<DialogResult> ShowDialogAsync(ViewModelBase viewModel);
    }

    public interface IDialogService<TViewModel> : IDialogService
        where TViewModel : ViewModelBase
    {
        DialogResult ShowDialog(TViewModel viewModel);
        Task<DialogResult> ShowDialogAsync(TViewModel viewModel);
    }

    class DialogService<TDialog, TViewModel> : DialogService<TDialog>, IDialogService<TViewModel>
        where TViewModel : ViewModelBase
        where TDialog : Window, new()
    {
        public DialogResult ShowDialog(TViewModel viewModel)
        {
            return base.ShowDialog(viewModel);
        }

        public Task<DialogResult> ShowDialogAsync(TViewModel viewModel)
        {
            return base.ShowDialogAsync(viewModel);
        }
    }
    class DialogService<TDialog> : IDialogService
        where TDialog : Window, new()
    {
        public DialogResult ShowDialog(ViewModelBase viewModel)
        {
            TDialog dialog = new()
            {
                DataContext = viewModel
            };

            if (Application.Current.MainWindow != null
               && Application.Current.MainWindow != dialog)
            {
                dialog.Owner = Application.Current.MainWindow;
            }

            if (dialog.ShowDialog() ?? false)
            {
                return DialogResult.OK;
            }

            return DialogResult.Cancel;
        }

        public async Task<DialogResult> ShowDialogAsync(ViewModelBase viewModel)
        {
            TDialog dialog = new()
            {
                DataContext = viewModel
            };

            if (Application.Current.MainWindow != null
               && Application.Current.MainWindow != dialog)
            {
                dialog.Owner = Application.Current.MainWindow;
            }

            if ((await dialog.ShowDialogAsync()) ?? false)
            {
                return DialogResult.OK;
            }

            return DialogResult.Cancel;
        }
    }
}
