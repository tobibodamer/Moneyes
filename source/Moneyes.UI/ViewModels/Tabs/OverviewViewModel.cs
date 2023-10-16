using CsvHelper;
using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    partial class OverviewViewModel : TabViewModelBase
    {
        private LiveDataService _liveDataService;
        private IExpenseIncomeService _expenseIncomeService;
        private readonly IBankingService _bankingService;

        decimal _totalExpense;
        decimal _totalIncome;
        decimal _averageExpense;
        decimal _averageIncome;
        private Balance _currentBalance;
        public ICommand LoadedCommand { get; }

        public ICommand ExportCommand { get; }

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

                if (Selector.DateSelection is not View.DateSelectionMode.Month)
                {
                    return;
                }

                UpdateCategories().FireAndForgetSafeAsync();
            }
        }

        private bool? _expenditureType;
        public bool? ExpenditureType
        {
            get => _expenditureType;
            set
            {
                _expenditureType = value;
                OnPropertyChanged();
                UpdateCategories().FireAndForgetSafeAsync();
            }
        }

        public ExpenseCategoriesViewModel ExpenseCategories { get; }
        public OverviewViewModel(
            LiveDataService liveDataService,
            IExpenseIncomeService expenseIncomeService,
            IUniqueCachedRepository<TransactionDbo> transactionRepository,
            IBankingService bankingService,
            ExpenseCategoriesViewModel expenseCategoriesViewModel,
            SelectorViewModel selectorViewModel)
        {
            DisplayName = "Overview";

            ExpenseCategories = expenseCategoriesViewModel;
            Selector = selectorViewModel;
            _liveDataService = liveDataService;
            _expenseIncomeService = expenseIncomeService;
            _bankingService = bankingService;

            NeedsUpdate = true;

            Selector.SelectorChanged += async (sender, args) =>
            {
                if (PostponeUpdate())
                {
                    return;
                }

                await UpdateCategories();
            };

            transactionRepository.RepositoryChanged += async (args) =>
            {
                if (PostponeUpdate())
                {
                    return;
                }

                await UpdateCategories();
            };

            ExportCommand = new RelayCommand(ExportTransactions);
        }

        private static DateTime FirstOfMonth(DateTime date)
        {
            return new(date.Year, date.Month, 1);
        }

        private TransactionFilter GetFilter()
        {
            TransactionType transactionType = ExpenditureType switch
            {
                true => TransactionType.Expense,
                false => TransactionType.Income,
                null => TransactionType.None
            };

            if (ShowAverage && Selector.DateSelection is View.DateSelectionMode.Month)
            {
                return new TransactionFilter()
                {
                    AccountNumber = Selector.CurrentAccount?.Number,
                    TransactionType = transactionType
                };
            }

            return new TransactionFilter()
            {
                AccountNumber = Selector.CurrentAccount?.Number,
                StartDate = Selector.FromDate,
                EndDate = Selector.EndDate,
                TransactionType = transactionType
            };
        }

        private async Task UpdateCategories()
        {
            _ = _expenseIncomeService.GetTotalExpenditure(GetFilter() with { TransactionType = TransactionType.None })
                .OnSuccess(result =>
                {
                    TotalExpense = result.TotalExpense;
                    AverageExpense = result.GetMonthlyAverage(TransactionType.Expense);

                    TotalIncome = result.TotalIncome;
                    AverageIncome = result.GetMonthlyAverage(TransactionType.Income);
                });

            await ExpenseCategories.UpdateCategories(GetFilter(), CategoryTypes.Real | CategoryTypes.NoCategory, order: true);

            if (Selector.CurrentAccount == null)
            {
                CurrentBalance = new Balance(Guid.Empty) { Amount = _bankingService.GetOverallBalance(Selector.EndDate) };
            }
            else
            {
                CurrentBalance = _bankingService.GetBalance(Selector.EndDate, Selector.CurrentAccount);
            }
        }

        public override void OnSelect()
        {
            base.OnSelect();

            if (NeedsUpdate)
            {
                UpdateCategories()
                    .ContinueWith(t => NeedsUpdate = false)
                    .FireAndForgetSafeAsync();
            }
        }

        private void ExportTransactions()
        {
            var totalExpenditureResult = _expenseIncomeService.GetTotalExpenditure(
                filter: GetFilter(), 
                absolute: false);

            var categoryExpenditureResult = _expenseIncomeService.GetExpenditurePerCategory(
                filter: GetFilter(), 
                categories: CategoryTypes.Real | CategoryTypes.NoCategory, 
                includeSubCategories: true, 
                absolute: false);

            var totalExpenditure = totalExpenditureResult.Data;
            var categoryExpenses = categoryExpenditureResult.Data.OrderBy(x => x.Category.Name)
                                                                 .OrderByDescending(x => x.Category.IsReal);

            var tempFile = Path.GetTempFileName();
            using (var writer = new StreamWriter(tempFile))
            using (var csv = new CsvWriter(writer, CultureInfo.CurrentCulture))
            {
                csv.WriteField("");

                for (DateTime d = FirstOfMonth(totalExpenditure.StartDate); d <= FirstOfMonth(totalExpenditure.EndDate); d = d.AddMonths(1))
                {
                    csv.WriteField(d.ToString("dd/MM"));
                }

                csv.WriteField("Sum");
                csv.WriteField("Average");

                csv.NextRecord();

                void writeExpenditure(string name, IExpenditure ex, TransactionType type = TransactionType.None)
                {
                    csv.WriteField(name);

                    var startMonth = FirstOfMonth(ex.StartDate);
                    var endMonth = FirstOfMonth(ex.EndDate);

                    for (DateTime d = startMonth; d <= endMonth; d = d.AddMonths(1))
                    {
                        if (ex.MonthlyExpenses.TryGetValue(d, out var monthEx))
                        {
                            csv.WriteField(Math.Round(monthEx.GetAmountByType(type), 2));
                        }
                        else
                        {
                            csv.WriteField("");
                        }
                    }

                    if (ex.Transactions.Any())
                    {
                        // Sum
                        csv.WriteField(Math.Round(ex.GetAmountByType(type), 2));

                        // Average
                        csv.WriteField(Math.Round(ex.GetMonthlyAverage(type), 2));
                    }
                    else
                    {
                        csv.WriteField("");
                        csv.WriteField("");
                    }

                    csv.NextRecord();
                }


                // Categories
                foreach (var (category, expenditure) in categoryExpenses)
                {
                    writeExpenditure(category.Name, expenditure);
                }

                csv.NextRecord();

                // Total
                writeExpenditure("Total Expense", totalExpenditure, TransactionType.Expense);
                writeExpenditure("Total Income", totalExpenditure, TransactionType.Income);
                writeExpenditure("Total Amount", totalExpenditure);
            }

            Process.Start("notepad", tempFile);
        }
    }
}
