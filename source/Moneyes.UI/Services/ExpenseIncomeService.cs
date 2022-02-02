using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Moneyes.UI
{
    partial class ExpenseIncomServieUsingDb : IExpenseIncomeService
    {
        private readonly ITransactionService _transactionService;
        private readonly ICategoryService _categoryService;

        public ExpenseIncomServieUsingDb(
            ITransactionService transactionervice,
            ICategoryService categoryService)
        {
            _transactionService = transactionervice ?? throw new ArgumentNullException(nameof(transactionervice));
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        }

        public Result<Expenses> GetExpenses(
            Category category, TransactionFilter filter, bool includeSubCategories = false)
        {
            return GetInternal(category, filter, includeSubCategories, TransactionType.Expense);
        }

        public Result<Expenses> GetIncome(
            Category category, TransactionFilter filter, bool includeSubCategories = false)
        {
            return GetInternal(category, filter, includeSubCategories, TransactionType.Income);
        }

        private Result<Expenses> GetInternal(Category category, TransactionFilter filter, bool includeSubCategories,
            TransactionType transactionType)
        {
            try
            {
                List<Category> categories = new() { category };

                if (includeSubCategories)
                {
                    categories.AddRange(_categoryService.GetSubCategories(category));
                }

                // Set filter to only include expenses
                filter.TransactionType = transactionType;

                // Get transactions for filter and categories
                List<Transaction> transactions = _transactionService.All(filter, categories.ToArray())
                    .ToList();

                // Find total start and end date
                DateTime startDate = filter.StartDate ?? _transactionService.EarliestTransactionDate(filter);
                DateTime endDate = filter.EndDate ?? _transactionService.LatestTransactionDate(filter);

                return Result.Successful(new Expenses(transactions)
                {
                    StartDate = startDate,
                    EndDate = endDate
                });
            }
            catch
            {
                return Result.Failed<Expenses>();
            }
        }

        public Result<IEnumerable<(Category Category, Expenses Expenses)>> GetAllExpenses(
            TransactionFilter filter,
            CategoryTypes categoryFlags = CategoryTypes.Real | CategoryTypes.NoCategory,
            bool includeSubCategories = false)
        {
            return GetAllInternal(filter, categoryFlags, includeSubCategories, TransactionType.Expense);
        }

        public Result<IEnumerable<(Category Category, Expenses Expenses)>> GetAllIncome(
            TransactionFilter filter,
            CategoryTypes categoryFlags = CategoryTypes.Real | CategoryTypes.NoCategory,
            bool includeSubCategories = false)
        {
            return GetAllInternal(filter, categoryFlags, includeSubCategories, TransactionType.Income);
        }

        private Result<IEnumerable<(Category Category, Expenses Expenses)>> GetAllInternal(
            TransactionFilter filter,
            CategoryTypes categoryFlags,
            bool includeSubCategories,
            TransactionType transactionType)
        {
            try
            {
                List<Category> categories = _categoryService.GetCategories(categoryFlags).ToList();

                List<(Category, Expenses)> results = new();

                foreach (Category c in categories)
                {
                    _ = GetInternal(c, filter, includeSubCategories, transactionType)
                        .OnSuccess(expenses =>
                        {
                            results.Add((c, expenses));
                        });
                }

                return Result.Successful(results.AsEnumerable());
            }
            catch
            {
                return Result.Failed<IEnumerable<(Category, Expenses)>>();
            }
        }

        public Result<Expenses> GetTotalExpense(TransactionFilter filter)
        {
            return GetTotalInternal(filter, TransactionType.Expense);
        }

        public Result<Expenses> GetTotalIncome(TransactionFilter filter)
        {
            return GetTotalInternal(filter, TransactionType.Income);
        }
        private Result<Expenses> GetTotalInternal(TransactionFilter filter, TransactionType transactionType)
        {
            try
            {
                // Set filter to only include expenses
                filter.TransactionType = transactionType;

                // Get transactions for filter and categories
                List<Transaction> transactions = _transactionService.All(filter)
                    .ToList();

                // Find total start and end date
                DateTime startDate = filter.StartDate ?? _transactionService.EarliestTransactionDate(filter);
                DateTime endDate = filter.EndDate ?? _transactionService.LatestTransactionDate(filter);

                return Result.Successful(new Expenses(transactions)
                {
                    StartDate = startDate,
                    EndDate = endDate
                });
            }
            catch
            {
                return Result.Failed<Expenses>();
            }
        }
    }
}