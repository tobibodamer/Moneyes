using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.LiveData;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{    internal class EditCategoryViewModel : CategoryViewModel
    {

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

        public ICommand ApplyCommand { get; set; }

        public bool Validate(ICategoryService categoryService)
        {
            var existingCategory = categoryService.GetCategoryByName(Name).GetOrNull();

            if (existingCategory != null)
            {
                if (IsCreated && existingCategory.Id == Category.Id)
                {
                    // Update existing
                }
                else
                {
                    // Already exists
                    return false;
                }
            }

            return true;
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

        public decimal? _minAmount;
        public decimal? MinAmount
        {
            get => _minAmount;
            set
            {
                _minAmount = value;
                OnPropertyChanged();
            }
        }

        public decimal? _maxAmount;
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

        public EditCategoryViewModel()
        {
        }
    }
}
