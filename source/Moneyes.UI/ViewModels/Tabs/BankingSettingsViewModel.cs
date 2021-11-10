using Moneyes.Data;
using Moneyes.LiveData;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    class BankingSettingsViewModel : ViewModelBase, ITabViewModel, INotifyDataErrorInfo
    {
        private readonly LiveDataService _liveDataService;
        private readonly IBankingService _bankingService;

        #region UI 

        private int? _bankCode;
        public int? BankCode
        {
            get => _bankCode;
            set
            {
                if (value == _bankCode) { return; }

                _bankCode = value;
                BankLookupCompleted = false;
                IsDirty = true;

                OnPropertyChanged(nameof(BankCode));
                FindBankCommand?.RaiseCanExecuteChanged();
            }
        }

        private string _bankLookupResult;
        public string BankLookupResult
        {
            get => _bankLookupResult;
            set
            {
                _bankLookupResult = value;

                OnPropertyChanged(nameof(BankLookupResult));
            }
        }

        private bool _bankLookupCompleted;
        public bool BankLookupCompleted
        {
            get => _bankLookupCompleted;
            set
            {
                _bankLookupCompleted = value;

                OnPropertyChanged(nameof(BankLookupCompleted));
            }
        }

        private bool _bankFound;
        public bool BankFound
        {
            get => _bankFound;
            set
            {
                _bankFound = value;

                OnPropertyChanged(nameof(BankFound));
            }
        }

        private string _userId;
        public string UserId
        {
            get => _userId;
            set
            {
                if (value == _userId)
                {
                    return;
                }
                if (_errors.TryRemove(nameof(UserId), out _))
                {
                    OnErrorsChanged(nameof(UserId));
                }

                _userId = value;
                IsDirty = true;
                OnPropertyChanged(nameof(UserId));
            }
        }

        private SecureString _pin;
        public SecureString PIN
        {
            get => _pin;
            set
            {
                if (_pin == null && value == null)
                {
                    return;
                }

                _pin = value;
                IsDirty = true;
                OnPropertyChanged(nameof(PIN));
            }
        }

        private bool _isDirty;
        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                _isDirty = value;
                OnPropertyChanged(nameof(IsDirty));
                ApplyCommand?.RaiseCanExecuteChanged();
            }
        }
        public AsyncCommand FindBankCommand { get; }
        public AsyncCommand ApplyCommand { get; }
        public AsyncCommand LoadedCommand { get; }

        #endregion

        public BankingSettingsViewModel(LiveDataService liveDataService, IBankingService bankingService)
        {
            DisplayName = "Settings";

            _liveDataService = liveDataService;
            _bankingService = bankingService;

            LoadedCommand = new AsyncCommand(async ct =>
            {
                
            });

            FindBankCommand = new AsyncCommand(async ct =>
            {
                await Task.Run(() => LookupBank());
            }, () => BankCode.HasValue);

            ApplyCommand = new AsyncCommand(async ct =>
            {
                await ApplySettings();
            }, () => IsDirty);
        }

        private OnlineBankingDetails CreateBankingDetails()
        {
            return new()
            {
                BankCode = BankCode.Value,
                UserId = UserId,
                Pin = PIN
            };
        }

        private async Task ApplySettings()
        {
            Validate();

            if (HasErrors)
            {
                return;
            }

            if (!BankLookupCompleted)
            {
                await FindBankCommand.ExecuteAsync();
            }

            if (!BankFound)
            {
                return;
            }

            OnlineBankingDetails bankingDetails = CreateBankingDetails();

            if (!PIN.IsNullOrEmpty())
            {
                Result result = await _liveDataService.CreateBankConnection(bankingDetails, testConnection: true);

                if (!result.IsSuccessful)
                {
                    MessageBox.Show("Could not connect to bank. Check your bank code and credentials.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // If sync was established -> save configuration
            _bankingService.BankingDetails = bankingDetails;
        } 

        public void LookupBank()
        {
            _ = _liveDataService.FindBank(_bankCode.Value)
                     .OnSuccess(bank =>
                     {
                         BankLookupResult = bank.Institute;
                         BankFound = true;
                     })
                     .OnError(() =>
                     {
                         BankLookupResult = "Bank not supported.";
                         BankFound = false;
                     });

            BankLookupCompleted = true;
        }

        public void OnSelect()
        {
            if (_bankingService.HasBankingDetails)
            {
                OnlineBankingDetails bankingDetails = _bankingService.BankingDetails;

                // Set UI fields
                BankCode = bankingDetails.BankCode;
                UserId = bankingDetails.UserId;
                PIN = bankingDetails.Pin;

                IsDirty = false;

                Task.Run(() => LookupBank());
            }
        }

        #region Validation

        private ConcurrentDictionary<string, List<string>> _errors =
            new ConcurrentDictionary<string, List<string>>();
        public void Validate()
        {
            _errors = new();

            if (string.IsNullOrEmpty(UserId))
            {
                _errors.TryAdd(nameof(UserId), new() { "Cannot be empty" });
                OnErrorsChanged(nameof(UserId));
            }

            OnPropertyChanged(nameof(HasErrors));
        }
        public bool HasErrors
        {
            get
            {
                return _errors.Any(kv => kv.Value != null && kv.Value.Count > 0);
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
