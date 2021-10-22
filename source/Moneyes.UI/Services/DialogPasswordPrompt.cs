using Moneyes.UI.View;
using Moneyes.UI.ViewModels;
using System.Security;
using System.Threading.Tasks;

namespace Moneyes.UI
{
    class DialogPasswordPrompt : IPasswordPrompt
    {
        public Task<SecureString> WaitForPasswordAsync()
        {
            PasswordDialogViewModel viewModel = new();
            PasswordDialog dialog = new() { DataContext = viewModel };

            if (dialog.ShowDialog() ?? false)
            {
                return Task.FromResult(viewModel.Password);
            }

            return Task.FromResult<SecureString>(null);
        }
    }
}
