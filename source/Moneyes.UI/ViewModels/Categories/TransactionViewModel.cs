using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using Moneyes.UI.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    /// <summary>
    /// A simple view model for a <see cref="Core.Transaction"/>.
    /// </summary>
    internal class TransactionViewModel : ViewModelBase
    {
        private Transaction _transaction;
        public Transaction Transaction
        {
            get
            {
                return _transaction;
            }
        }

        public TransactionViewModel(Transaction transaction)
        {
            _transaction = transaction;
        }
    }
}
