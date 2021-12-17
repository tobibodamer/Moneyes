using Moneyes.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.UI.ViewModels
{
    public class ImportAccountsViewModel : ViewModelBase
    {
        private ObservableCollection<AccountViewModel> _accounts;
        public ObservableCollection<AccountViewModel> Accounts
        {
            get => _accounts;
            set
            {
                _accounts = value;

                OnPropertyChanged();
            }
        }

        internal IEnumerable<AccountDetails> SelectedAccounts =>
            Accounts.Where(a => a.IsSelected).Select(a => a.Account);

        public ImportAccountsViewModel(IEnumerable<AccountDetails> accounts)
        {
            Accounts = new(accounts.Select(acc =>
                new AccountViewModel()
                {
                    Account = acc,
                    IsSelected = true
                }));
        }

        public class AccountViewModel : ViewModelBase
        {
            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
            public AccountDetails Account { get; init; }
        }
    }
}
