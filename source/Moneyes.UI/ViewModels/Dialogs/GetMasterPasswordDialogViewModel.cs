using Moneyes.Core;
using System;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    class GetMasterPasswordDialogViewModel : ViewModelBase, IDialogViewModel
    {
        public ICommand ApplyCommand { get; }

        public ICommand CancelCommand { get; }

        public event EventHandler<RequestCloseDialogEventArgs> RequestClose;

        public SecureString Password { get; set; } = string.Empty.ToSecuredString();

        public GetMasterPasswordDialogViewModel()
        {
            CancelCommand = new AsyncCommand(ct =>
            {
                RequestClose?.Invoke(this, new() { Result = false });
                return Task.CompletedTask;
            });

            ApplyCommand = new AsyncCommand(ct =>
            {
                RequestClose?.Invoke(this, new() { Result = true });
                return Task.CompletedTask;
            });
        }
    }
}
