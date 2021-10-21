using Moneyes.UI.View;
using Moneyes.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Moneyes.UI
{
    public interface IPasswordPrompt
    {
        Task<SecureString> WaitForPasswordAsync();
    }
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

            return null;
        }
    }
}
