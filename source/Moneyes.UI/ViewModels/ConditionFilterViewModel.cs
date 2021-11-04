using Moneyes.Core;
using Moneyes.Core.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    public class ConditionFilterViewModel : ViewModelBase
    {
        #region Static members

        private static readonly IDictionary<string, string> _filterProperties = GetFilterProperties();
        private static readonly IDictionary<string, ConditionOperator> _operators = GetOperators();
        public static IEnumerable<string> FilterProperties { get; } = _filterProperties.Keys;
        public static IEnumerable<string> Operators { get; } = _operators.Keys;
        private static IDictionary<string, string> GetFilterProperties()
        {
            return typeof(Transaction).GetProperties()
                .Where(p => p.GetCustomAttributes(typeof(FilterPropertyAttribute), false).Any())
                .ToDictionary(p =>
                    (p.GetCustomAttributes(typeof(FilterPropertyAttribute), false).First() as FilterPropertyAttribute).DescriptiveName,
                    p => p.Name);
        }
        private static IDictionary<string, ConditionOperator> GetOperators()
        {
            return Enum.GetValues<ConditionOperator>().ToDictionary(v => v.GetDescription(), v => v);
        }

        #endregion

        #region UI

        private string _property;
        public string Property
        {
            get
            {
                return _property;
            }
            set
            {
                _property = value;
                OnPropertyChanged();
            }
        }

        private string _conditionOperator;
        public string Operator
        {
            get
            {
                return _conditionOperator;
            }
            set
            {
                _conditionOperator = value;
                OnPropertyChanged();
            }
        }

        private List<object> _content;
        public List<object> Content
        {
            get
            {
                return _content;
            }
            set
            {
                _content = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddCommand { get; set; }
        public ICommand DeleteCommand { get; set; }

        #endregion

        public ConditionFilterViewModel()
        {
            Property = FilterProperties.First();
            Operator = ConditionOperator.Contains.GetDescription();
        }
        public ConditionFilterViewModel(IConditionFilter<Transaction> condition)
        {
            Property = _filterProperties.FirstOrDefault(name => name.Value.Equals(condition.Selector)).Key;
            Operator = condition.Operator.GetDescription();
            Content = condition.Values.Cast<object>().ToList();
        }

        /// <summary>
        /// Gets whether the properties are valid for a condition filter.
        /// </summary>
        public bool IsValid =>
            !string.IsNullOrEmpty(Property) && _filterProperties.ContainsKey(Property) &&
            !string.IsNullOrEmpty(Operator) && _operators.ContainsKey(Operator) &&
            Content != null && Content.Any();

        /// <summary>
        /// Gets a condition filter object from this view model.
        /// </summary>
        /// <returns></returns>
        public IConditionFilter<Transaction> GetFilter()
        {
            if (!IsValid)
            {
                return null;
            }

            ConditionOperator op = _operators[Operator];
            string propName = _filterProperties[Property];

            IConditionFilter<Transaction> filter = ConditionFilters.Create<Transaction>(propName);

            filter.Operator = op;
            filter.Values = Content.ToList();

            return filter;
        }
    }
}
