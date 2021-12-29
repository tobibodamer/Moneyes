using System;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    public interface IDialogViewModel
    {
        event EventHandler<RequestCloseDialogEventArgs> RequestClose;
        ICommand ApplyCommand { get; }
        ICommand CancelCommand { get; }
    }

    public class RequestCloseDialogEventArgs : EventArgs
    {
        public bool Result { get; set; }
    }
}