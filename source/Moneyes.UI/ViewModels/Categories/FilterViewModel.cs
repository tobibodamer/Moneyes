using Moneyes.Core;
using Moneyes.Core.Filters;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    public class FilterViewModel : ViewModelBase
    {
        private bool _hasFilter;
        public bool HasFilter
        {
            get
            {
                return _hasFilter;
            }
            set
            {
                _hasFilter = value;
                OnPropertyChanged();
            }
        }

        private LogicalOperator _logicalOperator;
        public LogicalOperator LogicalOperator
        {
            get
            {
                return _logicalOperator;
            }
            set
            {
                _logicalOperator = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<ConditionFilterViewModel> _conditionFilters = new();
        public ObservableCollection<ConditionFilterViewModel> Conditions => _conditionFilters;

        private ObservableCollection<FilterViewModel> _childFilters = new();
        public ObservableCollection<FilterViewModel> ChildFilters
        {
            get
            {
                return _childFilters;
            }
            set
            {
                _childFilters = value;
                OnPropertyChanged();
            }
        }
        public int TotalChildrenCount
        {
            get
            {
                return ChildFilters.Count + Conditions.Count;
            }
        }

        /// <summary>
        /// Command to delete this filter
        /// </summary>
        public ICommand DeleteCommand { get; set; }

        /// <summary>
        /// Command to add a new child filter
        /// </summary>
        public ICommand AddCommand { get; set; }

        public FilterViewModel(FilterGroup<Transaction> filterGroup = null)
        {
            if (filterGroup == null)
            {
                LogicalOperator = LogicalOperator.Or;

                // Add default condition
                _conditionFilters.Add(CreateConditionViewModel());
            }
            else
            {
                if (filterGroup.Conditions == null || filterGroup.Conditions.Count == 0)
                {
                    // Add default condition
                    _conditionFilters.Add(CreateConditionViewModel());
                }
                else
                {
                    _conditionFilters = new(filterGroup.Conditions.Select(c =>
                    {
                        return CreateConditionViewModel(c);
                    }));
                }

                LogicalOperator = filterGroup.Operator;

                AddChildFilters(filterGroup);
            }

            AddCommand = new AsyncCommand(async ct =>
            {
                AddChildFilterViewModel();
            });

            // Dont allow delete default filter
            DeleteCommand = new AsyncCommand(null, () => false);

            OnPropertyChanged(nameof(Conditions));
        }
        public FilterViewModel(TransactionFilter transactionFilter)
            : this(transactionFilter.Criteria)
        {
            if (transactionFilter == null) { return; }

            HasFilter = true;
        }

        /// <summary>
        /// Add all child filters of a given filter group as view models to this filter.
        /// </summary>
        /// <param name="filterGroup"></param>
        private void AddChildFilters(FilterGroup<Transaction> filterGroup)
        {
            if (filterGroup.ChildFilters == null) { return; }

            foreach (var childFilter in filterGroup.ChildFilters)
            {
                AddChildFilterViewModel(childFilter);
            }
        }

        /// <summary>
        /// Add a child filter view model to this filter, 
        /// or create and add a new filter view model if not supplied.
        /// </summary>
        /// <param name="childFilter"></param>
        private void AddChildFilterViewModel(FilterGroup<Transaction> childFilter = null)
        {
            FilterViewModel filterViewModel = childFilter == null ? new() : new(childFilter);

            // Set delete command for child filters
            filterViewModel.DeleteCommand = new AsyncCommand(async ct =>
            {
                ChildFilters.Remove(filterViewModel);
            },
            () => ChildFilters.Count > 0);

            ChildFilters.Add(filterViewModel);

            if (childFilter == null) { return; }

            // If created from model, add its child filters
            filterViewModel.AddChildFilters(childFilter);
        }

        /// <summary>
        /// Create a condition view model from a condition, or create an empty one from scratch.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private ConditionFilterViewModel CreateConditionViewModel(IConditionFilter<Transaction> c = null)
        {
            ConditionFilterViewModel conditionViewModel = c is not null ? new(c) : new();

            conditionViewModel.AddCommand = new AsyncCommand(async ct =>
            {
                _conditionFilters.Add(CreateConditionViewModel());
                OnPropertyChanged(nameof(Conditions));
            });

            conditionViewModel.DeleteCommand = new AsyncCommand(async ct =>
            {
                if (_conditionFilters.Remove(conditionViewModel))
                {
                    OnPropertyChanged(nameof(Conditions));
                }
            },
            () => _conditionFilters.Count > 1);

            return conditionViewModel;
        }

        /// <summary>
        /// Creates a <see cref="FilterGroup<Transaction>"/> from this filter view model.
        /// </summary>
        /// <returns></returns>
        public FilterGroup<Transaction> GetFilterGroup()
        {
            FilterGroup<Transaction> filterGroup = new(LogicalOperator);

            foreach (ConditionFilterViewModel conditionViewModel in Conditions
                .Where(condition => condition.IsValid))
            {
                filterGroup.AddCondition(conditionViewModel.GetFilter());
            }

            foreach (FilterViewModel filterViewModel in ChildFilters)
            {
                filterGroup.ChildFilters.Add(filterViewModel.GetFilterGroup());
            }

            return filterGroup;
        }

    }
}
