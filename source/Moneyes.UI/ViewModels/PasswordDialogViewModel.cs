using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    class PasswordDialogViewModel
    {
        public string Text { get; set; }
        public string Title { get; set; }
        public SecureString Password { get; set; }

        public ICommand OkCommand { get; }

        public ICommand CancelCommand { get; }

        public DialogResult DialogResult { get; set; }

        public PasswordDialogViewModel()
        {
            OkCommand = new AsyncCommand(ct =>
            {
                DialogResult = DialogResult.OK;

                return Task.CompletedTask;
            });

            CancelCommand = new AsyncCommand(ct =>
            {
                DialogResult = DialogResult.Cancel;

                return Task.CompletedTask;
            });
        }
    }
}
