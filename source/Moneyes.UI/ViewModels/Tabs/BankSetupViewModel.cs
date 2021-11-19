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

namespace Moneyes.UI.ViewModels
{
    public enum BankSetupState
    {
        None = 0,
        BankFound,
        BankNotFound,
        BankLookupFailed,
        ManualConfiguring,
        ConnectionFailed,
        ConnectionSuccessful
    }
    class BankSetupViewModel : ViewModelBase, ITabViewModel, INotifyDataErrorInfo
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

                Bank = null;
                BankLookupCompleted = false;
                State = BankSetupState.None;

                OnPropertyChanged();

                FindBankCommand?.RaiseCanExecuteChanged();
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

                if (State is BankSetupState.ConnectionSuccessful)
                {
                    if (BankLookupCompleted)
                    {
                        State = IsBankFound.Value ? BankSetupState.BankFound : BankSetupState.BankNotFound;
                    }
                }

                _userId = value;
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }

        private BankSetupState _state;
        public BankSetupState State
        {
            get => _state;
            set
            {
                _state = value;
                OnPropertyChanged();
            }
        }

        private SimpleBankViewModel _bank;
        public SimpleBankViewModel Bank
        {
            get => _bank;
            set
            {
                _bank = value;

                OnPropertyChanged();
            }
        }

        private bool _bankLookupCompleted;
        public bool BankLookupCompleted
        {
            get => _bankLookupCompleted;
            set
            {
                _bankLookupCompleted = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsBankFound));
            }
        }

        public bool? IsBankFound => BankLookupCompleted ? Bank != null : null;

        private bool _savePassword;
        public bool SavePassword
        {
            get => _savePassword;
            set
            {
                _savePassword = value;
                OnPropertyChanged();
            }
        }
        public AsyncCommand FindBankCommand { get; }
        public AsyncCommand ApplyCommand { get; }
        public AsyncCommand LoadedCommand { get; }

        #endregion

        public BankSetupViewModel(LiveDataService liveDataService, IBankingService bankingService)
        {
            DisplayName = "Settings";

            _liveDataService = liveDataService;
            _bankingService = bankingService;

            FindBankCommand = new AsyncCommand(async ct =>
            {
                await Task.Run(() => LookupBank());
            }, () => BankCode.HasValue);

            ApplyCommand = new AsyncCommand(async ct =>
            {
                await ApplySettings();
            }, () => (IsBankFound ?? false) && !string.IsNullOrEmpty(UserId));
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

            if (Bank is null)
            {
                //await FindBankCommand.ExecuteAsync();
                return;
            }

            OnlineBankingDetails bankingDetails = CreateBankingDetails();

            if (!PIN.IsNullOrEmpty())
            {
                Result result = await _liveDataService.CreateBankConnection(bankingDetails, testConnection: true);

                if (!result.IsSuccessful)
                {
                    State = BankSetupState.ConnectionFailed;
                    return;
                }
            }

            if (!SavePassword)
            {
                bankingDetails.Pin = null;
            }

            // If sync was established -> save configuration
            _bankingService.BankingDetails = bankingDetails;

            State = BankSetupState.ConnectionSuccessful;
        }

        public void LookupBank()
        {
            try
            {
                IBankInstitute bankInsitute = _liveDataService.FindBank(_bankCode.Value);

                if (bankInsitute != null)
                {
                    Bank = new()
                    {
                        BankName = bankInsitute.Name,
                        BankServer = new Uri(bankInsitute.FinTs_Url).Host,
                        BankVersion = bankInsitute.Version
                    };

                    State = BankSetupState.BankFound;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ApplyCommand.RaiseCanExecuteChanged();
                    });
                }
                else
                {
                    State = BankSetupState.BankNotFound;
                }
            }
            catch
            {
                State = BankSetupState.BankLookupFailed;
            }

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

                FindBankCommand.Execute(null);
            }
        }

        public class SimpleBankViewModel : ViewModelBase
        {
            private string _bankName;
            public string BankName
            {
                get => _bankName;
                set
                {
                    _bankName = value;

                    OnPropertyChanged();
                }
            }

            private string _bankServer;
            public string BankServer
            {
                get => _bankServer;
                set
                {
                    _bankServer = value;

                    OnPropertyChanged();
                }
            }

            private string _bankVersion;
            public string BankVersion
            {
                get => _bankVersion;
                set
                {
                    _bankVersion = value;

                    OnPropertyChanged();
                }
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

            OnPropertyChanged();
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
