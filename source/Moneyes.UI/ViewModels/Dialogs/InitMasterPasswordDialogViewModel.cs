using Moneyes.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    internal class InitMasterPasswordDialogViewModel : ViewModelBase, IDialogViewModel, INotifyDataErrorInfo
    {
        public ICommand ApplyCommand { get; }

        public ICommand CancelCommand { get; }

        public event EventHandler<RequestCloseDialogEventArgs> RequestClose;

        public SecureString Password { get; set; } = string.Empty.ToSecuredString();
        public SecureString ConfirmPassword { get; set; } = string.Empty.ToSecuredString();

        public bool IsSkipped { get; private set; }

        public InitMasterPasswordDialogViewModel()
        {
            CancelCommand = new AsyncCommand(ct =>
            {
                IsSkipped = true;

                RequestClose?.Invoke(this, new() { Result = false });

                return Task.CompletedTask;
            });

            ApplyCommand = new AsyncCommand(ct =>
            {
                Validate();

                if (HasErrors)
                {
                    return Task.CompletedTask;
                }

                RequestClose?.Invoke(this, new() { Result = true });

                return Task.CompletedTask;
            });
        }

        #region Validation

        private Dictionary<string, List<string>> _errors = new();
        public void Validate()
        {
            _errors = new();
            Error = null;

            if (Password.IsNullOrEmpty())
            {
                _errors.TryAdd(nameof(Password), new() { "Please enter a password" });
                Error = "Please enter a password";
            }
            else if (ConfirmPassword.IsNullOrEmpty())
            {
                _errors.TryAdd(nameof(ConfirmPassword), new() { "Please enter a password" });
                Error = "Please enter a password";
            }
            else if (!ConfirmPassword.ToUnsecuredString().Equals(Password.ToUnsecuredString(),
                StringComparison.Ordinal))
            {
                Error = "Passwords do not match";
                _errors.TryAdd(nameof(ConfirmPassword), new() { "Passwords must be the same" });
            }

            OnErrorsChanged(nameof(ConfirmPassword));
            OnErrorsChanged(nameof(Password));
            OnPropertyChanged(nameof(HasErrors));
        }
        public bool HasErrors
        {
            get
            {
                return _errors.Any(kv => kv.Value != null && kv.Value.Count > 0);
            }
        }

        private string _error;
        public string Error
        {
            get => _error;
            set
            {
                _error = value;
                OnPropertyChanged();
            }
        }

        protected void OnErrorsChanged(string propertyName)
        {
            var handler = ErrorsChanged;
            if (handler != null)
                handler(this, new DataErrorsChangedEventArgs(propertyName));
        }
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string propertyName)
        {
            List<string> errorsForName;
            _errors.TryGetValue(propertyName, out errorsForName);
            return errorsForName;
        }



        #endregion
    }
}
