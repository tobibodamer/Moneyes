using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    class OverviewViewModel : ViewModelBase, ITabViewModel
    {
        private LiveDataService _liveDataService;
        private IExpenseIncomeService _expenseIncomeService;
        private readonly TransactionRepository _transactionRepository;
        private readonly IBankingService _bankingService;

        private ObservableCollection<CategoryExpenseViewModel> _categories = new();
        decimal _totalExpense;
        decimal _totalIncome;
        public ICommand LoadedCommand { get; }

        public ObservableCollection<CategoryExpenseViewModel> Categories
        {
            get => _categories;
            set
            {
                _categories = value;
                OnPropertyChanged();
            }
        }

        public decimal TotalExpense
        {
            get => _totalExpense;
            set
            {
                _totalExpense = value;
                OnPropertyChanged();
            }
        }

        public decimal TotalIncome
        {
            get => _totalIncome;
            set
            {
                _totalIncome = value;
                OnPropertyChanged();
            }
        }

        private DateTime _fromDate;
        public DateTime FromDate
        {
            get => _fromDate;
            set
            {
                _fromDate = value;
                OnPropertyChanged();
            }
        }

        private DateTime _endDate;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged();
            }
        }

        public ICommand DateChangedCommand { get; }

        public ExpenseCategoriesViewModel ExpenseCategories { get; }
        public OverviewViewModel(
            LiveDataService liveDataService,
            IExpenseIncomeService expenseIncomeService,
            TransactionRepository transactionRepository,
            IBankingService bankingService,
            ExpenseCategoriesViewModel expenseCategoriesViewModel)
        {
            DisplayName = "Overview";

            ExpenseCategories = expenseCategoriesViewModel;
            _liveDataService = liveDataService;
            _expenseIncomeService = expenseIncomeService;
            _transactionRepository = transactionRepository;
            _bankingService = bankingService;

            LoadedCommand = new AsyncCommand(async ct =>
            {
                
            });

            FromDate = new(DateTime.Now.Year, DateTime.Now.Month, 1); ;
            EndDate = DateTime.Now;

            DateChangedCommand = new AsyncCommand(async ct =>
            {
                UpdateCategories();
            });
        }

        private TransactionFilter GetFilter()
        {
            return new TransactionFilter()
            {
                AccountNumber = _bankingService.GetAccounts().First().Number,
                StartDate = FromDate,
                EndDate = EndDate
            };
        }

        private void UpdateCategories()
        {
            _expenseIncomeService.GetTotalExpense(GetFilter())
                    .OnSuccess(total => TotalExpense = total);

            _expenseIncomeService.GetTotalIncome(GetFilter())
                .OnSuccess(total => TotalIncome = total);

            // Get expenses per category
            _expenseIncomeService.GetExpensePerCategory(GetFilter(), true)
                .OnError(() => { })
                .OnSuccess(expenses =>
                {
                    Categories.Clear();

                    foreach ((Category category, decimal amt) in expenses
                        .OrderBy(p => p.Category.Target == 0)
                        .ThenByDescending(p => p.Category == Category.NoCategory))
                    {

                        Categories.Add(
                            new CategoryExpenseViewModel(category, amt)
                            {
                            });
                    }

                    // Set sub categories
                    foreach (CategoryExpenseViewModel category in Categories)
                    {
                        Category parent = category.Category?.Parent;
                        if (parent == null) { continue; }

                        // Add category as sub category in parent
                        Categories.FirstOrDefault(c => c.Category.Equals(parent))
                            .SubCatgeories.Add(category);
                    }
                });
        }

        public void OnSelect()
        {
            Load();
        }

        void Load()
        {
            if (!_bankingService.HasBankingDetails)
            {
                // No bank connection configured -> show message?
                return;
            }

            //TODO: Remove
            UpdateCategories();
        }
    }
}
