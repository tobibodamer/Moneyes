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
    public class CategoryService
    {
        private readonly IRepository<Category> _categoryStore;

        public async Task<Result<Category>> GetCategoryByName(string name)
        {
            try
            {
                return await _categoryStore.GetItem(name);
            }
            catch (Exception)
            {
                return Result.Failed<Category>();
                //TODO: Log
            }
        }

        public async Task<Result<IEnumerable<Category>>> GetCategories()
        {
            try
            {
                var categories = await _categoryStore.GetAll();


                return Result.Successful(categories);
            }
            catch (Exception)
            {
                return Result.Failed<IEnumerable<Category>>();
                //TODO: Log
            }
        }
    }


    class ExpenseIncomServieUsingDb : IExpenseIncomeService
    {
        private readonly IBaseRepository<Category> _categoryRepo;
        private readonly TransactionRepository _transactionRepo;

        public ExpenseIncomServieUsingDb(IBaseRepository<Category> categoryStore, TransactionRepository transactionStore)
        {
            _categoryRepo = categoryStore ?? throw new ArgumentNullException(nameof(categoryStore));
            _transactionRepo = transactionStore ?? throw new ArgumentNullException(nameof(transactionStore));
        }

        public Result<IEnumerable<(Category Category, decimal TotalAmt)>> GetExpensePerCategory(
            AccountDetails account, bool includeNoCategory = true)
        {
            try
            {
                List<Category> categories = _categoryRepo.All()
                    .OrderBy(c => c.Name)
                    .OrderBy(c => c.IsExlusive ? 1 : 0)
                    .ToList();

                if (includeNoCategory)
                {
                    categories.Insert(0, Category.NoCategory);
                }

                List<(Category, decimal)> results = new();

                foreach (Category c in categories)
                {
                    TransactionFilter filter = new()
                    {
                        AccountNumber = account.Number,
                        TransactionType = TransactionType.Expense
                    };

                    List<Transaction> transactions = _transactionRepo.All(filter, c)
                        .ToList();

                    decimal sum = Math.Abs(transactions.Sum(t => t.Amount));

                    results.Add((c, sum));
                }

                //return results;

                return Result.Successful(results.AsEnumerable());

                //IEnumerable<Transaction> transactions = await _transactionRepo.GetAll();

                //Dictionary<string, decimal> categoryAmtMap = categories.ToDictionary(c => c.Name, c => 0m);
                //Dictionary<string, Category> categoryNameMap = categories.ToDictionary(c => c.Name, c => c);

                //if (includeNoCategory)
                //{
                //    categoryAmtMap.Add(Category.NoCategory.Name, 0);
                //    categoryNameMap.Add(Category.NoCategory.Name, Category.NoCategory);
                //}

                //foreach (Transaction t in transactions)
                //{
                //    if (t.Type != TransactionType.Expense) { continue; }
                //    if (includeNoCategory && (t.Categories == null || !t.Categories.Any()))
                //    {
                //        categoryAmtMap[Category.NoCategory.Name] += t.Amount;

                //        continue;
                //    }

                //    bool anyMatch = false;

                //    foreach (Category c in t.Categories)
                //    {
                //        if (!categoryAmtMap.ContainsKey(c.Name))
                //        {
                //            continue;
                //        }

                //        anyMatch = true;
                //        categoryAmtMap[c.Name] += t.Amount;
                //    }

                //    if (!anyMatch && includeNoCategory)
                //    {
                //        categoryAmtMap[Category.NoCategory.Name] += t.Amount;
                //    }
                //}

                //return Result.Successful(categoryAmtMap.Select(
                //    kv => (categoryNameMap[kv.Key], Math.Abs(kv.Value))));
            }
            catch
            {
                return Result.Failed<IEnumerable<(Category, decimal)>>();
            }
        }

        public Result<decimal> GetTotalExpense(AccountDetails account)
        {
            return GetTotalInternal(account, TransactionType.Expense);
        }
        public Result<decimal> GetTotalExpense(AccountDetails account, Category category)
        {
            if (category == Category.NoCategory)
            {
                return GetExpensePerCategory(account, true).Data
                    .First(item => item.Category == Category.NoCategory).TotalAmt;
            }

            return GetTotalInternal(account, category, TransactionType.Expense);
        }

        public Result<decimal> GetTotalIncome(AccountDetails account)
        {
            return GetTotalInternal(account, TransactionType.Income);
        }
        public Result<decimal> GetTotalIncome(AccountDetails account, Category category)
        {
            return GetTotalInternal(account, category, TransactionType.Income);
        }
        private Result<decimal> GetTotalInternal(AccountDetails account, TransactionType transactionType)
        {
            try
            {
                IEnumerable<Transaction> transactions = _transactionRepo
                    .All(new TransactionFilter { AccountNumber = account.Number });

                return Math.Abs(transactions
                    .Where(t => t.Type == transactionType)
                    .Sum(t => t.Amount));
            }
            catch
            {
                return Result.Failed<decimal>();
            }
        }

        private Result<decimal> GetTotalInternal(AccountDetails account, Category category, TransactionType transactionType)
        {
            try
            {
                IEnumerable<Transaction> transactions = _transactionRepo
                    .All(new TransactionFilter { AccountNumber = account.Number });

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
