using Moneyes.UI.View;
using Moneyes.UI.ViewModels;
using System.Security;
using System.Threading.Tasks;
using System.Windows;

namespace Moneyes.UI
{
    class DialogPasswordPrompt : IPasswordPrompt
    {
        string _title;
        string _text;
        public DialogPasswordPrompt(string title, string text)
        {
            _title = title;
            _text = text;
        }
        public Task<SecureString> WaitForPasswordAsync()
        {
            PasswordDialogViewModel viewModel = new()
            {
                Title = _title,
                Text = _text
            };

            PasswordDialog dialog = new()
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
                return Task.FromResult(viewModel.Password);
            }

            return Task.FromResult<SecureString>(null);
        }
    }
}
