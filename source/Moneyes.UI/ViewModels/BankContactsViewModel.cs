using Moneyes.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    public class BankContactsViewModel : ViewModelBase
    {
        private readonly IBankingService _bankingService;

        public BankContactsViewModel(IBankingService bankingService)
        {
            _bankingService = bankingService;
        }

        private BankContactViewModel _selectedBankConnection;
        public BankContactViewModel SelectedBankConnection
        {
            get => _selectedBankConnection;
            set
            {
                _selectedBankConnection = value;

                OnPropertyChanged();
            }
        }

        private ObservableCollection<BankContactViewModel> _bankConnections = new();
        public ObservableCollection<BankContactViewModel> BankConnections
        {
            get => _bankConnections;
            set
            {
                _bankConnections = value;

                OnPropertyChanged();
            }
        }

        public void UpdateBankConnections()
        {
            var bankConnections = _bankingService.GetBankEntries();

            var viewModels = bankConnections.Select(x => new BankContactViewModel(_bankingService)
            {
                Id = x.Id,
                BankName = x.Name,
                BankServer = x.Server,
                BankCode = x.BankCode,
                HbciVersion = x.HbciVersion,
                UserId = x.UserId,
                PIN = x.Pin
            });

            var selectedTemp = SelectedBankConnection?.Id;

            BankConnections.DynamicUpdate(viewModels, (b1, b2) => b1.Id == b2.Id);

            if (selectedTemp != null)
            {
                SelectedBankConnection = BankConnections.FirstOrDefault(x => x.Id == selectedTemp);
            }
            else
            {
                SelectedBankConnection = BankConnections.FirstOrDefault();
            }
        }
    }

    public class BankContactViewModel : ViewModelBase
    {
        public Guid Id { get; init; }

        private int _bankCode;
        public int BankCode
        {
            get => _bankCode;
            set
            {
                if (value == _bankCode) { return; }

                _bankCode = value;

                OnPropertyChanged();
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

        private int _bankVersion;
        public int HbciVersion
        {
            get => _bankVersion;
            set
            {
                _bankVersion = value;

                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; }

        public BankContactViewModel(IBankingService bankingService)
        {
            SaveCommand = new RelayCommand(() =>
            {
                bankingService.UpdateBankConnection(GetBankDetailsModel());
            });
        }

        public BankDetails GetBankDetailsModel()
        {
            return new(Id, BankCode)
            {
                HbciVersion = HbciVersion,
                Name = BankName,
                Server = BankServer,
                UserId = UserId,
                Pin = PIN,
            };
        }
    }
}
