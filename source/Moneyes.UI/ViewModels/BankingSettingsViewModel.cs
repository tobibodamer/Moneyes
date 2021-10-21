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
    class BankingSettingsViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        private readonly OnlineBankingDetails _onlineBankingDetails;
        private readonly LiveDataService _liveDataService;

        public BankingSettingsViewModel(LiveDataService liveDataService)
        {
            DisplayName = "Settings";

            _liveDataService = liveDataService;

            FindBankCommand = new AsyncCommand(ct =>
            {
                _liveDataService.FindBank(_bankCode.Value)
                    .OnSuccess(bank =>
                    {
                        BankLookupResult = bank.Institute;
                        BankFound = true;
                    })
                    .OnError(() =>
                    {
                        BankLookupResult = "Bank not supported.";
                        BankFound = false;

                        //_errors.TryAdd(nameof(BankCode), new());
                        //_errors[nameof(BankCode)].Add("Bank not supported.");
                        //OnErrorsChanged(nameof(BankCode));
                    });

                BankLookupCompleted = true;

                return Task.CompletedTask;
            }, () => BankCode.HasValue);

            ApplyCommand = new AsyncCommand(async ct =>
            {
                Validate();

                if (HasErrors)
                {
                    return;
                }

                if (!BankLookupCompleted)
                {
                    await FindBankCommand.ExecuteAsync();

                    if (BankFound == false)
                    {
                        return;
                    }
                }

                OnlineBankingDetails bankingDetails = CreateBankingDetails();

                Result result = await _liveDataService.Initialize(bankingDetails);

                if (!result.IsSuccessful)
                {
                    MessageBox.Show("Error");
                    return;
                }
                //TODO: PIN Prompt evtl.

                MessageBox.Show("Success");
            });
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

        private int? _bankCode;
        public int? BankCode
        {
            get => _bankCode;
            set
            {
                if (value == _bankCode) { return; }

                _bankCode = value;
                BankLookupCompleted = false;

                OnPropertyChanged(nameof(BankCode));
                FindBankCommand.RaiseCanExecuteChanged();
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

                if (_errors.TryRemove(nameof(UserId), out _))
                {
                    OnErrorsChanged(nameof(UserId));
                }

                _userId = value;
                OnPropertyChanged(nameof(UserId));
            }
        }

        private SecureString _pin;
        public SecureString PIN
        {
            get => _pin;
            set
            {
                _pin = value;
                OnPropertyChanged(nameof(PIN));
            }
        }
        public AsyncCommand FindBankCommand { get; }
        public AsyncCommand ApplyCommand { get; }


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
    }
}
