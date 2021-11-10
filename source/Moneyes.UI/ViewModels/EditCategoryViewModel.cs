using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using Moneyes.LiveData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    internal class EditCategoryViewModel : CategoryViewModel, INotifyDataErrorInfo, IDialogViewModel
    {
        private ICategoryService _categoryService;

        private List<Category> _possibleParents;
        public IEnumerable<Category> PossibleParents
        {
            get => _possibleParents;
            set
            {
                _possibleParents = value.ToList();
                OnPropertyChanged();
            }
        }

        public bool IsCreated { get; init; }
        public bool IsValid
        {
            get
            {
                Category existingCategory = _categoryService
                    .GetCategoryByName(Name).GetOrNull();

                return !string.IsNullOrEmpty(Name) && existingCategory is null ||
                    (IsCreated && existingCategory.Idquals(base.Category));
            }
        }
        public bool AssignTransactions { get; set; }

        public override Category Category
        {
            get
            {
                if (base.Category is null) { return null; }


                FilterGroup<Transaction> criteria = Filter.GetFilterGroup();
                TransactionFilter filter = new()
                {
                    TransactionType = this.TransactionType,
                    MinAmount = this.MinAmount,
                    MaxAmount = this.MaxAmount
                };

                if (criteria.ChildFilters.Any() || criteria.Conditions.Any())
                {
                    filter.Criteria = criteria;
                }

                if (filter.IsNull())
                {
                    filter = null;
                }

                return new Category
                {
                    Id = base.Category.Id,
                    Name = Name,
                    Parent = Parent,
                    Target = Target ?? 0,
                    Filter = filter
                };
            }
            set
            {
                base.Category = value;

                if (value != null && value.Filter != null)
                {
                    Filter = new(value.Filter);
                    TransactionType = value.Filter.TransactionType;
                }
                else
                {
                    Filter = new();
                }
            }
        }

        #region Filter

        private FilterViewModel _filter;
        public FilterViewModel Filter
        {
            get
            {
                return _filter;
            }
            set
            {
                _filter = value;
                OnPropertyChanged();
            }
        }

        private TransactionType _transactionType;
        public TransactionType TransactionType
        {
            get => _transactionType;
            set
            {
                _transactionType = value;
                OnPropertyChanged();
            }
        }

        private decimal? _minAmount;
        public decimal? MinAmount
        {
            get => _minAmount;
            set
            {
                _minAmount = value;
                OnPropertyChanged();
            }
        }

        private decimal? _maxAmount;
        public decimal? MaxAmount
        {
            get => _maxAmount;
            set
            {
                _maxAmount = value;
                OnPropertyChanged();
            }
        }

        #endregion
        public ICommand ApplyCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public event EventHandler<RequestCloseDialogEventArgs> RequestClose;

        public EditCategoryViewModel(ICategoryService categoryService)
        {
            _categoryService = categoryService;

            ApplyCommand = new AsyncCommand(async ct =>
            {
                if (!Validate())
                {
                    return;
                }

                Category category = Category;

                _categoryService.UpdateCategory(category);


                if (AssignTransactions)
                {
                    // Call method to assign transactions
                    _categoryService.AssignCategory(category);
                }

                RequestClose?.Invoke(this, new() { Result = true });
            });

            CancelCommand = new AsyncCommand(async ct =>
            {
                RequestClose?.Invoke(this, new() { Result = false });
            });
        }

        #region Validation

        private Dictionary<string, List<string>> _errors = new();
        public bool HasErrors => _errors.Any(kv => kv.Value != null && kv.Value.Count > 0);
        protected void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string propertyName)
        {
            _errors.TryGetValue(propertyName, out List<string> errorsForName);

            return errorsForName;
        }

        public bool Validate()
        {
            _errors = new();

            if (string.IsNullOrEmpty(Name))
            {
                _errors.TryAdd(nameof(Name), new() { "Cannot be empty" });
                OnErrorsChanged(nameof(Name));

                return false;
            }

            Category existingCategory = _categoryService
                .GetCategoryByName(Name).GetOrNull();

            if (existingCategory != null)
            {
                if (IsCreated && existingCategory.Idquals(Category))
                {
                    // Update existing
                }
                else
                {
                    // Already exists

                    _errors.TryAdd(nameof(Name), new() { "Category with this name already exists" });
                    OnErrorsChanged(nameof(Name));
                    OnPropertyChanged(nameof(HasErrors));

                    return false;
                }
            }

            OnPropertyChanged(nameof(HasErrors));

            return true;
        }

        #endregion
    }
}
