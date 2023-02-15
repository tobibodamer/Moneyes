using LiteDB;
using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using Moneyes.UI;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Moneyes.Test
{
    public abstract class TransactionServiceTestBase
    {
        protected readonly TransactionFactory _transactionFactory;
        protected readonly ICategoryService _categoryService;
        protected readonly IReadOnlyList<Category> _categories;
        protected readonly IReadOnlyList<Transaction> _transactions;
        protected readonly ILiteDatabase _database;
        protected readonly ILiteCollection<TransactionDbo> _transactionsCollection;
        protected readonly IUniqueCachedRepository<TransactionDbo> _transactionRepo;
        protected readonly ITransactionService _transactionService;

        public TransactionServiceTestBase()
        {
            _transactionFactory = new TransactionFactory(new CategoryFactory(new FilterFactory()));
            _categoryService = Substitute.For<ICategoryService>();

            _categories = CreateCategories().ToList();
            _transactions = CreateTransactions(_categories).ToList();
            _database = CreateDatabase();
            _transactionsCollection = CreateTransactionsCollection(_transactions, _database);
            _transactionRepo = CreateTransactionsRepo(_database);

            _categoryService.GetCategories(Arg.Any<CategoryTypes>()).Returns(_categories);

            _transactionService = new TransactionService(_transactionRepo, _transactionFactory, _categoryService);
        }

        private static IEnumerable<Transaction> CreateTransactions(IReadOnlyList<Category> categories)
        {
            // Transaction without category, and no category matches this
            yield return new Transaction(Guid.NewGuid())
            {
                Category = null,
                Index = 0,
            };

            // Transaction with category having matching the this same
            yield return new Transaction(Guid.NewGuid())
            {
                Category = categories[1],
                Index = 1,
            };

            // Transaction with category having no filter, but a different category matches this
            yield return new Transaction(Guid.NewGuid())
            {
                Category = categories[0],
                Index = 2,
            };

            // Transaction with category, and no category matches this
            yield return new Transaction(Guid.NewGuid())
            {
                Category = categories[2],
                Index = 3,
            };

            // Transaction without category, but a category exists that matches this
            yield return new Transaction(Guid.NewGuid())
            {
                Category = null,
                Index = 4,
            };
        }

        private static IEnumerable<Category> CreateCategories()
        {
            yield return new Category(Guid.NewGuid())
            {
                Name = "Category0"
            };

            yield return CreateCategoryWithFilter(
                "Category1",
                Guid.NewGuid(),
                filter => filter.Criteria.AddCondition(transaction => transaction.Index, ConditionOperator.Equal, 1)
                );

            yield return CreateCategoryWithFilter(
                "Category2",
                Guid.NewGuid(),
                filter =>
                {
                    filter.Criteria.AddCondition(transaction => transaction.Index, ConditionOperator.Equal, 2);
                    filter.Criteria.AddCondition(transaction => transaction.Index, ConditionOperator.Equal, 4);
                }
                );

            yield return CreateCategoryWithFilter(
                "Category3",
                Guid.NewGuid(),
                filter =>
                {
                    filter.Criteria.AddCondition(transaction => transaction.Index, ConditionOperator.Equal, 500);
                }
                );
        }

        private static Category CreateCategoryWithFilter(string name, Guid id, Action<TransactionFilter> configureFilter)
        {
            var filter = new TransactionFilter();

            filter.Criteria.Operator = LogicalOperator.Or;

            configureFilter(filter);

            return new Category(id)
            {
                Name = name,
                Filter = filter
            };
        }

        private static ILiteDatabase CreateDatabase()
        {
            return new LiteDatabase(":memory:");
        }

        private static ILiteCollection<TransactionDbo> CreateTransactionsCollection(IReadOnlyList<Transaction> transactions, ILiteDatabase database)
        {
            var transactionsCollection = database.GetCollection<TransactionDbo>("Transactions");
            transactionsCollection.Insert(transactions.Select(t => t.ToDbo()));

            return transactionsCollection;
        }

        private static IUniqueCachedRepository<TransactionDbo> CreateTransactionsRepo(ILiteDatabase db)
        {
            var dbProvider = Substitute.For<IDatabaseProvider<ILiteDatabase>>();
            dbProvider.IsDatabaseCreated.Returns(true);
            dbProvider.IsOpen.Returns(true);
            dbProvider.Database.Returns(db);

            return new UniqueCachedRepository<TransactionDbo>(
               dbProvider,
               x => x.Id,
               new() { CollectionName = "Transactions", PreloadCache = true },
               new DependencyRefreshHandler()
               );
        }
    }
}
