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
                    .OnSuccess(total => TotalExpense = total);

            _expenseIncomeService.GetTotalIncome(GetFilter())
                .OnSuccess(total => TotalIncome = total);

            ExpenseCategories.UpdateCategories(GetFilter(), CategoryFlags.Real | CategoryFlags.NoCategory, order: true);

            CurrentBalance = _bankingService.GetBalance(Selector.EndDate, Selector.CurrentAccount);

            //// Get expenses per category
            //_expenseIncomeService.GetExpensePerCategory(GetFilter(), true)
            //    .OnError(() => { })
            //    .OnSuccess(expenses =>
            //    {
            //        Categories.Clear();

            //        foreach ((Category category, decimal amt) in expenses
            //            .OrderBy(p => p.Category.Target == 0)
            //            .ThenByDescending(p => p.Category == Category.NoCategory))
            //        {

            //            Categories.Add(
            //                new CategoryExpenseViewModel(category, amt)
            //                {
            //                });
            //        }

            //        // Set sub categories
            //        foreach (CategoryExpenseViewModel category in Categories)
            //        {
            //            Category parent = category.Category?.Parent;
            //            if (parent == null) { continue; }

            //            // Add category as sub category in parent
            //            Categories.FirstOrDefault(c => c.Category.Equals(parent))
            //                .SubCatgeories.Add(category);
            //        }
            //    });
        }

        public void OnSelect()
        {
            Selector.RefreshAccounts();
            UpdateCategories();
        }
    }
}
