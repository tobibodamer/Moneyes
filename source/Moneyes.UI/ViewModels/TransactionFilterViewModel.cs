using Moneyes.Core;
using Moneyes.Core.Filters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.UI.ViewModels
{
    public class TransactionFilterViewModel : ViewModelBase
    {
        private string _searchTerm;
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                _searchTerm = value;
                OnPropertyChanged();
                OnFilterChanged();
            }
        }

        private bool _caseSensitive;
        public bool CaseSensitive
        {
            get => _caseSensitive;
            set
            {
                _caseSensitive = value;
                OnPropertyChanged();
                OnFilterChanged();
            }
        }

        private bool _filterExpenses;
        public bool FilterExpenses
        {
            get => _filterExpenses;
            set
            {
                _filterExpenses = value;
                OnPropertyChanged();
                OnFilterChanged();
            }
        }

        private bool _filterIncome;
        public bool FilterIncome
        {
            get => _filterIncome;
            set
            {
                _filterIncome = value;
                OnPropertyChanged();
                OnFilterChanged();
            }
        }

        private ObservableCollection<FilterPropertyViewModel> _filterByProperties = new();
        public ObservableCollection<FilterPropertyViewModel> FilterByProperties
        {
            get => _filterByProperties;
            set
            {
                _filterByProperties = value;
                OnPropertyChanged();
                OnFilterChanged();
            }
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
                OnFilterChanged();
            }
        }

        public event EventHandler FilterChanged;

        public FilterGroup<Transaction> GetFilter()
        {
            FilterGroup<Transaction> filterGroup = new(LogicalOperator.And);

            var transactionTypeGroup = filterGroup.AddFilter(LogicalOperator.Or);

            if (FilterExpenses)
            {
                transactionTypeGroup.AddCondition(t => t.Type, ConditionOperator.Equal, TransactionType.Expense);
            }

            if (FilterIncome)
            {
                transactionTypeGroup.AddCondition(t => t.Type, ConditionOperator.Equal, TransactionType.Income);
            }

            if (string.IsNullOrEmpty(_searchTerm))
            {
                return filterGroup;
            }

            var searchGroup = filterGroup.AddFilter(LogicalOperator.Or);

            foreach (var prop in FilterByProperties)
            {
                IConditionFilter<Transaction> conditionFilter = ConditionFilters.Create<Transaction>(prop.Name);

                conditionFilter.Operator = ConditionOperator.Contains;
                conditionFilter.Values = new List<string>() { _searchTerm };
                conditionFilter.CaseSensitive = _caseSensitive;

                searchGroup.AddCondition(conditionFilter);
            }

            return filterGroup;
        }

        protected void OnFilterChanged()
        {
            FilterChanged?.Invoke(this, EventArgs.Empty);
        }

        public TransactionFilterViewModel()
        {
            FilterByProperties.Add(new()
            {
                Name = nameof(Transaction.Name),
                IsSelected = true
            });

            FilterByProperties.Add(new()
            {
                Name = nameof(Transaction.Purpose),
                IsSelected = true
            });

            FilterByProperties.Add(new()
            {
                Name = nameof(Transaction.AltName),
                IsSelected = true
            });
        }

        public class FilterPropertyViewModel : ViewModelBase
        {
            public string Name { get; init; }

            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
