using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.LiveData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    class CategoriesViewModel : ViewModelBase
    {
        private ObservableCollection<CategoryViewModel> _categories;
        public ObservableCollection<CategoryViewModel> Categories
        {
            get
            {
                return _categories;
            }
            set
            {
                _categories = value;
                OnPropertyChanged();
            }
        }

        EditCategoryViewModel _editCategoryViewModel;
        EditCategoryViewModel _addCategoryViewModel;
        public EditCategoryViewModel EditCategoryViewModel
        {
            get
            {
                return _editCategoryViewModel;
            }
            set
            {
                _editCategoryViewModel = value;
                OnPropertyChanged();
            }
        }

        public EditCategoryViewModel AddCategoryViewModel
        {
            get
            {
                return _addCategoryViewModel;
            }
            set
            {
                _addCategoryViewModel = value;
                OnPropertyChanged();
            }
        }
        public ICommand AddCommand { get; }

        CategoryViewModelFactory _factory;
        protected ICategoryService CategoryService { get; }
        protected ITransactionService TransactionService { get; }
        public CategoriesViewModel(CategoryViewModelFactory factory, ICategoryService categoryService)
        {
            _factory = factory;
            CategoryService = categoryService;

            AddCommand = new AsyncCommand(async ct =>
            {
                AddCategoryViewModel = _factory.CreateAddCategoryViewModel();
            });
        }
        public virtual void UpdateCategories()
        {
            foreach (Category category in CategoryService.GetCategories().Data)
            {
                Categories.Add(
                    _factory.CreateCategoryViewModel(category, editViewModel =>
                    {
                        EditCategoryViewModel = editViewModel;
                    }));
            }
        }
    }

    class ExpenseCategoriesViewModel : CategoriesViewModel
    {
        CategoryViewModelFactory _factory;
        IExpenseIncomeService _expenseIncomeService;
        public ExpenseCategoriesViewModel(CategoryViewModelFactory factory,
            ICategoryService categoryService,
            IExpenseIncomeService expenseIncomeService)
            : base(factory, categoryService)
        {
            _expenseIncomeService = expenseIncomeService;
            _factory = factory;
        }

        private CategoryExpenseViewModel _selectedCategory;
        public CategoryExpenseViewModel SelectedCategory
        {
            get
            {
                return _selectedCategory;
            }
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<CategoryExpenseViewModel> _categories = new();
        public new ObservableCollection<CategoryExpenseViewModel> Categories
        {
            get
            {
                return _categories;
            }
            set
            {
                base.Categories = new(value);
                _categories = value;
                OnPropertyChanged();
            }
        }

        public override void UpdateCategories()
        {
            throw new NotImplementedException();
        }

        public void UpdateCategories(AccountDetails account)
        {
            // Get expenses per category
            _expenseIncomeService.GetExpensePerCategory(account)
                .OnError(() => { })//HandleError("Could not get expenses for this category"))
                .OnSuccess(expenses =>
                {
                    int? selectedCategoryId = SelectedCategory?.Category?.Id;

                    Categories.Clear();

                    foreach ((Category category, decimal amt) in expenses)
                    {
                        AddEntry(category, amt);
                    }

                    // Get total expenses
                    _expenseIncomeService.GetTotalExpense(account)
                                    .OnSuccess(totalAmt =>
                                    {
                                        AddEntry(Category.AllCategory, totalAmt);
                                    })
                                    .OnError(() => { }); //HandleError("Could not get total expense"));

                    // Set sub categories
                    foreach (CategoryExpenseViewModel category in Categories)
                    {
                        Category parent = category.Category?.Parent;
                        if (parent == null) { continue; }

                        // Add category as sub category in parent
                        Categories.FirstOrDefault(c => c.Category.Equals(parent))
                            .SubCatgeories.Add(category);
                    }

                    if (selectedCategoryId.HasValue)
                    {
                        CategoryExpenseViewModel previouslySelectedCategory = Categories
                            .FirstOrDefault(c => c.Category.Id == selectedCategoryId);

                        if (previouslySelectedCategory != null)
                        {
                            previouslySelectedCategory.IsSelected = true;
                        }
                    }
                    else
                    {
                        CategoryExpenseViewModel allCategory = Categories
                            .FirstOrDefault(c => c.Category == Category.AllCategory);

                        if (allCategory != null)
                        {
                            allCategory.IsSelected = true;
                            OnPropertyChanged(nameof(SelectedCategory));
                        }
                    }
                });
        }
        public void AddEntry(Category category, decimal expense)
        {
            Categories.Add(
                new CategoryExpenseViewModel(category, expense)
                {
                    AssignToTransaction = new AsyncCommand<Transaction>(async (transaction, ct) =>
                    {
                        Category targetCategory = category;
                        Category currentCategory = SelectedCategory?.Category;

                        if (TransactionService.MoveToCategory(transaction, currentCategory, targetCategory))
                        {
                            //UpdateCategories();
                            //UpdateTransactions();
                        }

                        await Task.CompletedTask;
                    },
                    (transaction) =>
                    {
                        Category targetCategory = category;

                        // cant change null transaction
                        if (transaction == null) { return false; }

                        // cant add to 'All' category
                        if (targetCategory == Category.AllCategory) { return false; }

                        // cant add to own category
                        if (transaction.Categories.Contains(targetCategory)) { return false; }

                        return true;
                    }),
                    EditCommand = new AsyncCommand(async ct =>
                    {
                        EditCategoryViewModel = _factory.CreateEditCategoryViewModel(category);
                    }),
                    DeleteCommand = new AsyncCommand(async ct =>
                    {
                        CategoryService.DeleteCategory(category);
                    })
                });
        }

        //public void UpdateCategories(IEnumerable<(Category category, decimal expense)> categories)
        //{
        //    foreach ((Category category, decimal expense) in categories)
        //    {
        //        Categories.Add(
        //            new CategoryExpenseViewModel(category, expense)
        //            {
        //                AssignToTransaction = new AsyncCommand<Transaction>(async (transaction, ct) =>
        //                {
        //                    Category targetCategory = category;
        //                    Category currentCategory = SelectedCategory?.Category;

        //                    if (TransactionService.MoveToCategory(transaction, currentCategory, targetCategory))
        //                    {
        //                        UpdateCategories();
        //                        UpdateTransactions();
        //                    }

        //                    await Task.CompletedTask;
        //                },
        //                (transaction) =>
        //                {
        //                    Category targetCategory = category;

        //                    // cant change null transaction
        //                    if (transaction == null) { return false; }

        //                    // cant add to 'All' category
        //                    if (targetCategory == Category.AllCategory) { return false; }

        //                    // cant add to own category
        //                    if (transaction.Categories.Contains(targetCategory)) { return false; }

        //                    return true;
        //                }),
        //                EditCommand = new AsyncCommand(async ct =>
        //                {
        //                    Category targetCategory = category;

        //                    var editCategoryViewModel = new EditCategoryViewModel();

        //                    editCategoryViewModel.ApplyCommand = new AsyncCommand(async ct =>
        //                    {
        //                        if (!editCategoryViewModel.Validate(_))
        //                        {
        //                            return;
        //                        }

        //                        if (!categoryService.UpdateCategory(Category))
        //                        {
        //                            return;
        //                        }

        //                        if (AssignTransactions)
        //                        {

        //                        }
        //                    });


        //                    //_editCategoryDialogService.ShowDialog(EditCategory);
        //                })
        //            });
        //    }
        //}
    }

    class CategoryViewModelFactory
    {
        public CategoryViewModelFactory(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }
        public CategoryViewModel CreateCategoryViewModel(Category category, Action<EditCategoryViewModel> editAction)
        {
            var categoryViewModel = new CategoryViewModel
            {
                Category = category,
                EditCommand = new AsyncCommand(async ct =>
                {
                    EditCategoryViewModel editViewModel = CreateEditCategoryViewModel(category);

                    editAction?.Invoke(editViewModel);
                })
            };

            categoryViewModel.DeleteCommand = new AsyncCommand(async ct =>
            {
                _categoryService.DeleteCategory(category);
            });

            return categoryViewModel;
        }

        public EditCategoryViewModel CreateAddCategoryViewModel()
        {
            Category newCategory = new();

            return CreateEditCategoryViewModel(newCategory, isCreated: false);
        }
        public EditCategoryViewModel CreateEditCategoryViewModel(Category category)
        {
            return CreateEditCategoryViewModel(category, isCreated: true);
        }

        private EditCategoryViewModel CreateEditCategoryViewModel(Category category, bool isCreated)
        {
            var editCategoryViewModel = new EditCategoryViewModel()
            {
                Category = category,
                IsCreated = isCreated
            };

            editCategoryViewModel.ApplyCommand = new AsyncCommand(async ct =>
            {
                if (!editCategoryViewModel.Validate(_categoryService))
                {
                    return;
                }

                if (!_categoryService.UpdateCategory(editCategoryViewModel.Category))
                {
                    return;
                }

                if (editCategoryViewModel.AssignTransactions)
                {
                    // Call method to assign transactions
                }
            });

            return editCategoryViewModel;
        }

        ICategoryService _categoryService;
    }

    internal class CategoryExpenseViewModel : CategoryViewModel
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

        private decimal _totalExpense;
        public decimal TotalExpense
        {
            get => _totalExpense;
            set
            {
                _totalExpense = value;
                OnPropertyChanged();
            }
        }

        public List<CategoryExpenseViewModel> SubCatgeories { get; set; } = new();

        public ICommand AssignToTransaction { get; set; }

        public bool IsOver => Target > 0 && TotalExpense > Target;

        public decimal Difference => Math.Abs(Target - TotalExpense);

        public CategoryExpenseViewModel(Category category, decimal totalExpense)
        {
            Category = category;
            TotalExpense = totalExpense;

            if (IsNoCategory)
            {
                DisplayName = $"-- No category -- ({totalExpense} €)";
            }
            else if (category.Target > 0)
            {

                DisplayName = $"{category.Name} ({totalExpense} / {category.Target} €)";
            }
            else
            {
                DisplayName = $"{category.Name} ({totalExpense} €)";
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    /// <summary>
    /// A simple view model for a <see cref="Core.Category"/>.
    /// </summary>
    internal class CategoryViewModel : ViewModelBase
    {
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

        private decimal _target;
        public decimal Target
        {
            get => _target;
            set
            {
                _target = value;
                OnPropertyChanged();
            }
        }

        private CategoryViewModel _parent;
        public CategoryViewModel Parent
        {
            get => _parent;
            set
            {
                _parent = value;
                OnPropertyChanged();
            }
        }

        private Category _category;

        public virtual Category Category
        {
            get
            {
                return _category;
            }
            set
            {
                _category = value;

                Name = _category.Name;
                Target = _category.Target;

                if (_category.Parent is not null)
                {
                    Parent = new CategoryViewModel { Category = _category.Parent };
                }
            }
        }

        public ICommand EditCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public bool IsNoCategory => _category == Category.NoCategory;

        public CategoryViewModel()
        {
        }
    }
    internal class EditCategoryViewModel : CategoryViewModel
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
                TransactionFilter filter = null;

                if (criteria.ChildFilters.Any() || criteria.Conditions.Any())
                {
                    filter = new()
                    {
                        Criteria = criteria
                    };
                }

                return new Category
                {
                    Id = base.Category.Id,
                    Name = Name,
                    Parent = Parent?.Category,
                    Target = Target,
                    Filter = filter
                };
            }
            set
            {
                base.Category = value;

                if (value != null && value.Filter != null)
                {
                    Filter = new(value.Filter);
                }
                else
                {
                    Filter = new();
                }
            }
        }

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

        public EditCategoryViewModel()
        {
        }
    }

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
