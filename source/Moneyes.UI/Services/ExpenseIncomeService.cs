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
    class ExpenseIncomServieUsingDb : IExpenseIncomeService
    {
        private readonly IBaseRepository<Category> _categoryRepo;
        private readonly TransactionRepository _transactionRepo;
        private readonly ICategoryService _categoryService;

        public ExpenseIncomServieUsingDb(
            IBaseRepository<Category> categoryStore, TransactionRepository transactionStore,
            ICategoryService categoryService)
        {
            _categoryRepo = categoryStore ?? throw new ArgumentNullException(nameof(categoryStore));
            _transactionRepo = transactionStore ?? throw new ArgumentNullException(nameof(transactionStore));
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        }

        public Result<IEnumerable<(Category Category, decimal TotalAmt)>> GetExpensePerCategory(
            AccountDetails account, bool includeNoCategory = true)
        {
            TransactionFilter filter = new()
            {
                AccountNumber = account.Number,
                TransactionType = TransactionType.Expense
            };

            return GetExpensePerCategory(filter, includeNoCategory);
        }

        public Result<IEnumerable<(Category Category, decimal TotalAmt)>> GetExpensePerCategory(
            TransactionFilter filter, bool includeNoCategory = true, bool includeSubCategories = false)
        {
            try
            {
                List<Category> categories = _categoryRepo.GetAll()
                    .OrderBy(c => c.Name)
                    .OrderBy(c => c.IsExlusive ? 1 : 0)
                    .ToList();

                if (includeNoCategory)
                {
                    categories.Insert(0, Category.NoCategory);
                }

                filter.TransactionType = TransactionType.Expense;

                List<(Category, decimal)> results = new();
                
                foreach (Category c in categories)
                {
                    List<Category> subCategories = _categoryService.GetSubCategories(c).ToList();
                    subCategories.Add(c);

                    List<Transaction> transactions = _transactionRepo.All(filter,
                            includeSubCategories ? subCategories.ToArray() : new Category[] { c })
                        .ToList();

                    decimal sum = Math.Abs(transactions.Sum(t => t.Amount));

                    results.Add((c, sum));
                }

                return Result.Successful(results.AsEnumerable());
            }
            catch
            {
                return Result.Failed<IEnumerable<(Category, decimal)>>();
            }
        }

        public Result<decimal> GetTotalExpense(TransactionFilter filter)
        {
            return GetTotalInternal(filter, TransactionType.Expense);
        }
        public Result<decimal> GetTotalExpense(TransactionFilter filter, Category category)
        {
            if (category == Category.NoCategory)
            {
                return GetExpensePerCategory(filter, true).Data
                    .First(item => item.Category == Category.NoCategory).TotalAmt;
            }

            return GetTotalInternal(filter, category, TransactionType.Expense);
        }

        public Result<decimal> GetTotalIncome(TransactionFilter filter)
        {
            return GetTotalInternal(filter, TransactionType.Income);
        }
        public Result<decimal> GetTotalIncome(TransactionFilter filter, Category category)
        {
            return GetTotalInternal(filter, category, TransactionType.Income);
        }
        private Result<decimal> GetTotalInternal(TransactionFilter filter, TransactionType transactionType)
        {
            try
            {
                IEnumerable<Transaction> transactions = _transactionRepo
                    .All(filter);

                return Math.Abs(transactions
                    .Where(t => t.Type == transactionType)
                    .Sum(t => t.Amount));
            }
            catch
            {
                return Result.Failed<decimal>();
            }
        }

        private Result<decimal> GetTotalInternal(TransactionFilter filter, Category category, TransactionType transactionType)
        {
            try
            {
                IEnumerable<Transaction> transactions = _transactionRepo
                    .All(filter);

                return Math.Abs(transactions.Where(t =>
                       t.Type == transactionType &&
                       (t.Categories?.Contains(category) ?? false))
                    .Sum(t => t.Amount));
            }
            catch
            {
                return Result.Failed<decimal>();
            }
        }
    }
}
