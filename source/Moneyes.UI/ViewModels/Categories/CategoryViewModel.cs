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
    /// A simple view model for a <see cref="Core.Category"/>.
    /// </summary>
    internal class CategoryViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        protected readonly ICategoryService _categoryService;

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        private decimal? _target;
        public decimal? Target
        {
            get => _target;
            set
            {
                _target = value;
                OnPropertyChanged();
            }
        }

        private Category _parent;
        public Category Parent
        {
            get => _parent;
            set
            {
                _parent = (value == Category.NoCategory) ? null : value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<CategoryViewModel> SubCatgeories { get; set; } = new();

        private Category _category;
        public Category Category
        {
            get
            {
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

                return new Category(_category?.Id ?? default)
                {
                    Name = Name,
                    Parent = Parent,
                    Target = Target ?? 0,
                    Filter = filter
                };
            }
            set
            {
                _category = value;

                Name = _category.Name;
                Target = _category.Target;

                if (_category.Parent is not null)
                {
                    Parent = _category.Parent;
                }

                if (value != null && value.Filter != null)
                {
                    Filter = new(value.Filter);
                    TransactionType = value.Filter.TransactionType;
                    MinAmount = value.Filter.MinAmount;
                    MaxAmount = value.Filter.MaxAmount;
                }
                else
                {
                    Filter = new();
                }
            }
        }


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

        public bool IsCreated { get; set; }
        public bool IsValid
        {
            get
            {
                Category existingCategory = _categoryService
                    .GetCategoryByName(Name);

                return (!string.IsNullOrEmpty(Name) && existingCategory is null) ||
                    (IsCreated && existingCategory.Id.Equals(Category.Id));
            }
        }

        public bool AssignTransactions { get; set; }
        public bool CanReassign { get; set; }

        public bool IsDirty => !IsCreated || (!_category?.Equals(Category) ?? true);

        public ICommand EditCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand ReassignCommand { get; set; }
        public ICommand SaveCommand { get; set; }


        public ICommand MoveToCategory { get; set; }
        public ICommand CopyToCategory { get; set; }

        public bool IsNoCategory => _category == Category.NoCategory;

        public bool IsRealCategory => !_category.Equals(Category.NoCategory)
            && !_category.Equals(Category.AllCategory);

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


        public CategoryViewModel(ICategoryService categoryService, IStatusMessageService statusMessageService)
        {
            _categoryService = categoryService;

            ReassignCommand = new RelayCommand(() =>
            {
                Category category = Category;

                int reassignedCount = _categoryService.AssignCategory(category);

                if (reassignedCount > 0)
                {
                    statusMessageService.ShowMessage($"{reassignedCount} transaction(s) reassigned");
                }
            }, () => CanReassign);

            SaveCommand = new RelayCommand(() =>
            {
                if (!Validate())
                {
                    return;
                }

                Category category = Category;

                if (_categoryService.UpdateCategory(category))
                {
                    statusMessageService.ShowMessage($"Category '{category.Name}' created");
                }
                else
                {
                    statusMessageService.ShowMessage($"Category '{category.Name}' saved");
                }

                if (CanReassign && AssignTransactions)
                {
                    ReassignCommand.Execute(null);
                }
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
                .GetCategoryByName(Name);

            if (existingCategory != null)
            {
                if (IsCreated && existingCategory.Id.Equals(Category.Id))
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
