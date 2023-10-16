using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using Moneyes.LiveData;

namespace Moneyes.UI
{
    public class ExpenseIncomServiceUsingDb : IExpenseIncomeService
    {
        private readonly ITransactionService _transactionService;
        private readonly ICategoryService _categoryService;

        public ExpenseIncomServiceUsingDb(
            ITransactionService transactionervice,
            ICategoryService categoryService)
        {
            _transactionService = transactionervice ?? throw new ArgumentNullException(nameof(transactionervice));
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        }

        public Result<IExpenditure> GetExpenditure(
            Category category, TransactionFilter filter, bool includeSubCategories, bool absolute)
        {
            return GetExpenditureInternal(category, filter, includeSubCategories, absolute);
        }

        public Result<IExpenditure> GetTotalExpenditure(TransactionFilter filter, bool absolute)
        {
            return GetExpenditureInternal(Category.AllCategory, filter, false, absolute);
        }

        private Result<IExpenditure> GetExpenditureInternal(Category category, TransactionFilter filter, bool includeSubCategories, bool absolute)
        {
            try
            {
                List<Category> categories = new() { category };

                if (includeSubCategories)
                {
                    categories.AddRange(_categoryService.GetSubCategories(category));
                }

                // Get transactions for filter and categories
                var accountTransactions = _transactionService.All(
                    new TransactionFilter()
                    {
                        AccountNumber = filter.AccountNumber
                    },
                    categories.ToArray()).ToList();

                var transactions = accountTransactions.Filter(filter);

                // Find total start and end date
                var (startDate, endDate) = GetDateRange(accountTransactions, filter.StartDate, filter.EndDate);

                var result = new Expenditure(transactions, absolute, startDate, endDate)
                {
                    StartDate = startDate,
                    EndDate = endDate
                };
                return result;
            }
            catch
            {
                return Result.Failed<IExpenditure>();
            }
        }

        private static (DateTime startDate, DateTime endDate) GetDateRange(IEnumerable<Transaction> accountFilteredTransactions, DateTime? startDate, DateTime? endDate)
        {
            var (et, lt) = accountFilteredTransactions.Select(t => t.BookingDate).MinAndMax();

            return (startDate > et ? startDate.Value : et,
                    endDate < lt ? endDate.Value : lt);
        }

        public Result<IEnumerable<(Category Category, IExpenditure Expenses)>> GetExpenditurePerCategory(
            TransactionFilter filter,
            CategoryTypes categoryFlags,
            bool includeSubCategories,
            bool absolute)
        {
            var categories = _categoryService.GetCategories(categoryFlags).ToDictionary(c => c.Id, c => c);
            var accountFilteredTransactions = _transactionService.All(
                    new TransactionFilter()
                    {
                        AccountNumber = filter.AccountNumber
                    }).ToList();

            var transactions = accountFilteredTransactions.Filter(filter);

            // Sort transactions per category
            var transactionsPerCategory = new Dictionary<Guid, List<Transaction>>();

            foreach (var c in categories.Keys)
            {
                transactionsPerCategory.Add(c, new());
            }

            foreach (var transaction in transactions)
            {
                var categoryId = transaction.Category?.Id ?? Category.NoCategoryId;
                var category = categories[categoryId];

                if (categoryFlags.HasFlag(CategoryTypes.All))
                {
                    transactionsPerCategory[Category.AllCategoryId].Add(transaction);
                }

                if (category == null)
                {
                    continue;
                }

                if (includeSubCategories)
                {
                    // Add transaction to every parent in branch
                    while (category != null)
                    {
                        transactionsPerCategory[category.Id].Add(transaction);
                        category = category.Parent;
                    }
                }
                else
                {
                    transactionsPerCategory[categoryId].Add(transaction);
                }
            }

            // Find total start and end date
            var (startDate, endDate) = GetDateRange(accountFilteredTransactions, filter.StartDate, filter.EndDate);

            var results = transactionsPerCategory
                .Select(kvp => (categories[kvp.Key],
                    new Expenditure(kvp.Value, absolute, startDate, endDate)
                    {
                        StartDate = startDate,
                        EndDate = endDate
                    } as IExpenditure))
                .ToList();

            return Result.Successful(results.AsEnumerable());
        }
    }
}