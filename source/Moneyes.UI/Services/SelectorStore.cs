using Moneyes.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.UI.Services
{
    class SelectorStore
    {
        private AccountDetails _currentAccount;
        public AccountDetails CurrentAccount
        {
            get => _currentAccount;
            set
            {
                if (_currentAccount == value) { return; }

                _currentAccount = value;
                AccountChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private DateTime _startDate;
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (_startDate == value) { return; }

                _startDate = value;
                DateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private DateTime _endDate;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (_endDate == value) { return; }

                _endDate = value;
                DateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler AccountChanged;
        public event EventHandler DateChanged;

    }
}
