using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using Moneyes.LiveData;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    partial class OverviewViewModel : ViewModelBase, ITabViewModel
    {
        private LiveDataService _liveDataService;
        private IExpenseIncomeService _expenseIncomeService;
        private readonly TransactionRepository _transactionRepository;
        private readonly IBankingService _bankingService;

        decimal _totalExpense;
        decimal _totalIncome;
        decimal _averageExpense;
        decimal _averageIncome;
        private Balance _currentBalance;
        public ICommand LoadedCommand { get; }

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

        public decimal AverageExpense
        {
            get => _averageExpense;
            set
            {
                _averageExpense = value;
                OnPropertyChanged();
            }
        }

        public decimal AverageIncome
        {
            get => _averageIncome;
            set
            {
                _averageIncome = value;
                OnPropertyChanged();
            }
        }

        private SelectorViewModel _selector;
        public SelectorViewModel Selector
        {
            get => _selector;
            set
            {
                _selector = value;
                OnPropertyChanged();
            }
        }

        public Balance CurrentBalance
        {
            get => _currentBalance;
            set
            {
                _currentBalance = value;
                OnPropertyChanged();
            }
        }

        private bool _showAverage;
        public bool ShowAverage
        {
            get => _showAverage;
            set
            {
                _showAverage = value;
                OnPropertyChanged();

                UpdateCategories();
            }
        }
        public ExpenseCategoriesViewModel ExpenseCategories { get; }
        public OverviewViewModel(
            LiveDataService liveDataService,
            IExpenseIncomeService expenseIncomeService,
            TransactionRepository transactionRepository,
            IBankingService bankingService,
            ExpenseCategoriesViewModel expenseCategoriesViewModel,
            SelectorViewModel selectorViewModel)
        {
            DisplayName = "Overview";

            ExpenseCategories = expenseCategoriesViewModel;
            Selector = selectorViewModel;
            _liveDataService = liveDataService;
            _expenseIncomeService = expenseIncomeService;
            _transactionRepository = transactionRepository;
            _bankingService = bankingService;

            Selector.SelectorChanged += (sender, args) =>
            {
                UpdateCategories();
            };

            _transactionRepository.EntityAdded += (transaction) =>
            {
                UpdateCategories();
            };

            _transactionRepository.EntityUpdated += (transaction) =>
            {
                UpdateCategories();
            };

            _transactionRepository.EntityDeleted += (transaction) =>
            {
                UpdateCategories();
            };
        }

        private TransactionFilter GetFilter()
        {
            if (ShowAverage)
            {
                return new TransactionFilter()
                {
                    AccountNumber = Selector.CurrentAccount?.Number
                };
            }

            return new TransactionFilter()
            {
                AccountNumber = Selector.CurrentAccount?.Number,
                StartDate = Selector.FromDate,
                EndDate = Selector.EndDate
            };
        }

        private void UpdateCategories()
        {
            _expenseIncomeService.GetTotalExpense(GetFilter())
                .OnSuccess(expenses =>
                {
                    TotalExpense = expenses.TotalAmount;
                    AverageExpense = expenses.GetMonthlyAverage();
                });

            _expenseIncomeService.GetTotalIncome(GetFilter())
                .OnSuccess(expenses =>
                {
                    TotalIncome = expenses.TotalAmount;
                    AverageIncome = expenses.GetMonthlyAverage();
                });

            ExpenseCategories.UpdateCategories(GetFilter(), CategoryTypes.Real | CategoryTypes.NoCategory, order: true);

            CurrentBalance = _bankingService.GetBalance(Selector.EndDate, Selector.CurrentAccount);
        }

        public void OnSelect()
        {
            Selector.RefreshAccounts();
            UpdateCategories();
        }
    }
}
