using Moneyes.Core;
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
        private readonly ITransactionService _transactionService;
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
        public OverviewViewModel(
            LiveDataService liveDataService,
            IExpenseIncomeService expenseIncomeService,
            ITransactionService transactionService,
            IBankingService bankingService)
        {
            DisplayName = "Overview";

            _liveDataService = liveDataService;
            _expenseIncomeService = expenseIncomeService;
            _transactionService = transactionService;
            _bankingService = bankingService;

            LoadedCommand = new AsyncCommand(async ct =>
            {
                if (!bankingService.HasBankingDetails)
                {
                    // No bank connection configured -> show message?
                    return;
                }

                //TODO: Remove
                UpdateCategories();

                _expenseIncomeService.GetTotalExpense(_bankingService.GetAccounts().First())
                    .OnSuccess(total => TotalExpense = total);

                _expenseIncomeService.GetTotalIncome(_bankingService.GetAccounts().First())
                    .OnSuccess(total => TotalIncome = total);
            });
        }

        private void UpdateCategories()
        {
            // Get expenses per category
            _expenseIncomeService.GetExpensePerCategory(_bankingService.GetAccounts().First(), true)
                .OnError(() => { })
                .OnSuccess(expenses =>
                {
                    Categories.Clear();

                    foreach ((Category category, decimal amt) in expenses
                        .OrderBy(p => p.Category.Target == 0)
                        .ThenByDescending(p => p.Category == Category.NoCategory))
                    {
                        //if (category.Target == 0)
                        //{
                        //    continue;
                        //}

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
    }
}
