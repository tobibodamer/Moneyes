using Moneyes.Core;
using Moneyes.LiveData;
using Moneyes.UI.Services;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    class BankSetupStepViewModel : ViewModelBase, INotifyDataErrorInfo, IWizardStepViewModel
    {
        private readonly LiveDataService _liveDataService;
        private readonly IBankingService _bankingService;
        private readonly IStatusMessageService _statusMessageService;

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
                if (_pin == value)
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

        private SimpleBankViewModel _bankConfiguration;
        public SimpleBankViewModel Bank
        {
            get => _bankConfiguration;
            set
            {
                _bankConfiguration = value;

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

        #endregion

        public ICommand CancelCommand => TransitionController?.FinishCommand;
        public ITransitionController TransitionController { get; set; }

        private BankDetails _bankDetails = null;

        public BankSetupStepViewModel(LiveDataService liveDataService, IBankingService bankingService, IStatusMessageService statusMessageService)
        {
            _liveDataService = liveDataService;
            _bankingService = bankingService;

            FindBankCommand = new AsyncCommand(async ct =>
            {
                await Task.Run(() => LookupBank(), ct);
            }, () => BankCode.HasValue);

            ApplyCommand = new AsyncCommand(async ct =>
            {
                await ApplySettings();
            }, () => (IsBankFound ?? false) && !string.IsNullOrEmpty(UserId));
            _statusMessageService = statusMessageService;
        }

        public Task OnTransitedFrom(TransitionContext transitionContext)
        {
            transitionContext.Argument = _bankDetails;
            return Task.CompletedTask;
        }

        public Task OnTransitedTo(TransitionContext transitionContext)
        {
            return Task.CompletedTask;
        }

        private OnlineBankingDetails CreateBankingDetails()
        {
            return new()
            {
                BankCode = BankCode.Value,
                Server = Bank.BankServerUri,
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

            if (PIN.IsNullOrEmpty())
            {
                return;
            }

            // Create connection details from view model properties
            OnlineBankingDetails onlineBankingDetails = CreateBankingDetails();

            // Test banking connection
            BankingResult result = await _liveDataService.TestConnection(onlineBankingDetails);

            if (!result.IsSuccessful)
            {
                State = BankSetupState.ConnectionFailed;

                if (result.ErrorCode is
                    OnlineBankingErrorCode.InvalidPin or
                    OnlineBankingErrorCode.InvalidUsernameOrPin)
                {
                    _statusMessageService.ShowMessage("Invalid credentials.");
                }

                return;
            }

            // Bank connection successful -> create bank details model
            BankDetails bankDetails = new(Guid.NewGuid(), onlineBankingDetails.BankCode)
            {
                UserId = onlineBankingDetails.UserId
            };

            if (SavePassword)
            {
                bankDetails.Pin = onlineBankingDetails.Pin;
            }

            // Add bank details model
            _bankingService.AddBankConnection(bankDetails);

            // Save temporarily so user doesnt have to reenter password in this session
            _liveDataService.SavePasswordTemporarily(bankDetails, onlineBankingDetails.Pin);

            _bankDetails = bankDetails;
            State = BankSetupState.ConnectionSuccessful;
            TransitionController?.NextStepCommand.Execute(null);
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
                        BankServer = bankInsitute.FinTs_Url,
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

            public Uri BankServerUri
            {
                get
                {
                    if ((Uri.TryCreate(BankServer, UriKind.Absolute, out var uri)) &&
                        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                    {
                        return uri;
                    }

                    return new UriBuilder(BankServer)
                    {
                        Scheme = Uri.UriSchemeHttps,
                        Port = -1
                    }.Uri;
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
