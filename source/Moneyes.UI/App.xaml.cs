﻿using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Core.Parsing;
using Moneyes.Data;
using Moneyes.LiveData;
using Moneyes.UI.Services;
using Moneyes.UI.View;
using Moneyes.UI.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;

namespace Moneyes.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IDatabaseProvider<ILiteDatabase> _dbProvider;

        private static void InitializeCultures()
        {
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(
                XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
        }

        private static string InitDatabasePath()
        {
            string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string dataDir = Path.Combine(userHome, ".moneyes");

            Directory.CreateDirectory(dataDir);

            return Path.Combine(dataDir, "database.db");
        }

        private static void SetupLogging()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logDir = Path.Combine(appData, @"Moneyes\logs");

            Directory.CreateDirectory(logDir);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Async(x => x.Debug(outputTemplate: "{Timestamp:HH:mm:ss} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}"))
                .WriteTo.File(path: Path.Combine(logDir, "log.txt"), rollingInterval: RollingInterval.Day)
                .MinimumLevel.Debug()
                .CreateLogger();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            RegisterGlobalExceptionHandling((ex, msg) =>
            {
                Log.Error(ex, msg);
            });

            InitializeCultures();

            SetupLogging();

            IServiceCollection services = new ServiceCollection();

            // Database
            services.AddLiteDb<UIDatabaseProvider>(config =>
                {
                    config.DatabasePath = InitDatabasePath();
                    config.BsonMapper.TrimWhitespace = false;
                })
                .AddCachedRepositories(builder =>
                {
                    builder.AddUniqueRepository<TransactionDbo>("Transaction", preloadCache: true)
                        .DependsOnOne(t => t.Category, "Category")
                        .WithUniqueProperty(t => t.UID);

                    builder.AddUniqueRepository<CategoryDbo>("Category", preloadCache: true)
                        .DependsOnOne(c => c.Parent, "Category")
                        .WithUniqueProperty(c => c.Name);

                    builder.AddUniqueRepository<AccountDbo>("Accounts", preloadCache: true)
                        .DependsOnOne(a => a.Bank, "BankDetails")
                        .WithUniqueProperty(a => a.IBAN)
                        .WithUniqueProperty(a => new { a.Number, a.Bank.Id });

                    builder.AddUniqueRepository<BalanceDbo>("Balance", preloadCache: true)
                        .DependsOnOne(b => b.Account, "Accounts")
                        .WithUniqueProperty(b => new { b.Date, b.Account.Id });

                    builder.AddUniqueRepository<BankDbo>("BankDetails", preloadCache: true)
                        .WithUniqueProperty(b => new { b.BankCode, b.UserId });
                });


            // Factories
            services.AddTransient<ICategoryFactory, CategoryFactory>();
            services.AddTransient<ITransactionFactory, TransactionFactory>();
            services.AddTransient<IFilterFactory, FilterFactory>();
            services.AddTransient<IAccountFactory, AccountFactory>();
            services.AddTransient<IBalanceFactory, BalanceFactory>();
            services.AddTransient<IBankDetailsFactory, BankDetailsFactory>();

            // Services

            services.AddTransient<IPasswordPrompt, OnlineBankingPasswordPrompt>();
            services.AddSingleton<IOnlineBankingServiceFactory, OnlineBankingServiceFactory>();

            services.AddScoped<LiveDataService>();
            services.AddScoped<IExpenseIncomeService, ExpenseIncomServiceUsingDb>();
            services.AddScoped<IBankingService, BankingService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<ICategoryService, CategoryService>();

            // UI services
            services.AddScoped<IStatusMessageService, StatusMessageService>();
            services.AddScoped<IDialogService<ImportAccountsViewModel>,
                DialogService<ImportAccountsView, ImportAccountsViewModel>>();

            services.AddScoped<IDialogService<EditCategoryViewModel>,
                DialogService<AddCategoryDialog, EditCategoryViewModel>>();

            services.AddScoped<IDialogService<GetMasterPasswordDialogViewModel>,
                DialogService<MasterPasswordDialog, GetMasterPasswordDialogViewModel>>();

            services.AddScoped<IDialogService<InitMasterPasswordDialogViewModel>,
                DialogService<InitMasterPasswordDialog, InitMasterPasswordDialogViewModel>>();

            services.AddScoped<SelectorStore>();
            services.AddSingleton<MasterPasswordProvider>(CreateMasterPasswordProvider);

            // View models
            services.AddTransient<ITabViewModel, OverviewViewModel>();
            services.AddTransient<ITabViewModel, TransactionsTabViewModel>();
            services.AddTransient<ITabViewModel, CategoriesTabViewModel>();
            //services.AddTransient<ITabViewModel, AddressBookViewModel>();
            services.AddTransient<ITabViewModel, AccountsViewModel>();
            services.AddTransient<ITabViewModel, SettingsTabViewModel>();

            services.AddTransient<SetupWizardViewModel>();

            services.AddScoped<CategoryViewModelFactory>();
            services.AddTransient<ExpenseCategoriesViewModel>();
            services.AddScoped<SelectorViewModel>();

            services.AddTransient<GetMasterPasswordDialogViewModel>();
            services.AddTransient<InitMasterPasswordDialogViewModel>();

            services.AddTransient<MainWindowViewModel>();


            services.AddLogging(builder => builder.AddSerilog());

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Open / create database
            _dbProvider = serviceProvider.GetRequiredService<IDatabaseProvider<ILiteDatabase>>();

            if (!_dbProvider.IsDatabaseCreated)
            {
                if (!await _dbProvider.TryCreateDatabase())
                {
                    Environment.Exit(-1);
                }
            }
            else
            {
                if (!await _dbProvider.TryOpenDatabase())
                {
                    Environment.Exit(-1);
                }
            }

            ApplyMigrations(_dbProvider.Database, serviceProvider.GetRequiredService<ILogger<ILiteDatabase>>());



            // Seed transactions:

            //var categories = categoryRepo.GetAll().ToList();
            //var toAdd = new List<TransactionDbo>();
            //for (int i = 0; i < 30000; i++)
            //{
            //    TransactionDbo t = new()
            //    {
            //        AltName = Random(10),
            //        BookingDate = new DateTime(2022, rnd.Next(1, 12), rnd.Next(1, 28)),
            //        Amount = rnd.Next(-3000, 3000) / 10.0m,
            //        IBAN = accountRepo.GetAll().First().IBAN,
            //        PartnerIBAN = Enumerable.Repeat(0, 20).Select(i => rnd.Next(0, 9)).ToString(),
            //        Currency = "EUR",
            //        Name = Random(12) + " " + Random(12),
            //        Id = Guid.NewGuid(),
            //        Purpose = Random(30),
            //        BIC = Random(12),
            //        BookingType = Random(10),
            //        UID = Guid.NewGuid().ToString(),
            //        Categories = Enumerable.Repeat(0, rnd.Next(3)).Select(i => categories[rnd.Next(categories.Count - 1)]).ToList()
            //    };
            //    toAdd.Add(t);
            //}

            //transactionRepo.Set(toAdd);



            //UpdateUIDs(transactionRepo);

            //RemoveDuplicates(
            //    transactionRepo,
            //    t => t.BookingDate.ToString() + t.BookingType + t.Name + t.Amount + t.Index + t.Purpose,
            //    t => t.PartnerIBAN != null, (t1, t2) => t1.PartnerIBAN == null ^ t2.PartnerIBAN == null);

            //RemoveDuplicates(
            //    transactionRepo,
            //    t => t.BookingDate.ToString() + t.BookingType + t.Name + t.Amount + t.Index + t.PartnerIBAN,
            //    t => t.Purpose != null, (t1, t2) => t1.Purpose == null ^ t2.Purpose == null);


            MainWindowViewModel mainWindowViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();

            this.MainWindow = new MainWindow(mainWindowViewModel);
            this.MainWindow.Show();

            this.MainWindow.Closed += (s, e) =>
            {
                Environment.Exit(0);
            };
        }

        private static Random rnd = new();
        private static string Random(int length)
        {
            string alphabet = "ABCDEFGHIJKGLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890";
            return new string(Enumerable.Repeat(0, length).Select(i => alphabet[rnd.Next(0, alphabet.Length)]).ToArray());
        }

        private static void ApplyMigrations(ILiteDatabase database, ILogger<ILiteDatabase> logger)
        {
            if (database.UserVersion == 0)
            {
                logger.LogInformation("Database version is 0. Migrating to version 1...");


                logger.LogInformation("Migrating Categories...");

                var categories = database.GetCollection("Category");
                var categoryDocuments = categories.FindAll().ToList();
                var categoryIdGuidMap = new Dictionary<int, Guid>();

                foreach (var document in categoryDocuments)
                {
                    categories.Delete(document["_id"]);
                    var guid = Guid.NewGuid();
                    categoryIdGuidMap.Add(document["_id"].AsInt32, guid);
                    document["_id"] = new BsonValue(guid);
                    document["CreatedAt"] = DateTime.UtcNow;
                    document["UpdatedAt"] = DateTime.UtcNow;
                    document["IsDeleted"] = false;
                }

                foreach (var document in categoryDocuments)
                {
                    if (!document.ContainsKey("Parent"))
                    {
                        continue;
                    }

                    var oldId = document["Parent"].AsDocument["$id"];
                    document["Parent"]["$id"] = categoryIdGuidMap[oldId];
                }

                categories.Insert(categoryDocuments);

                logger.LogInformation("Migrating Transactions...");

                var transactions = database.GetCollection("Transaction");
                var transactionDocuments = transactions.FindAll().ToList();


                foreach (var document in transactionDocuments)
                {
                    transactions.Delete(document["_id"]);
                    document["UID"] = new BsonValue(document["_id"].AsString);
                    document["_id"] = new BsonValue(Guid.NewGuid());
                    document["CreatedAt"] = DateTime.UtcNow;
                    document["UpdatedAt"] = DateTime.UtcNow;
                    document["IsDeleted"] = false;
                }

                foreach (var document in transactionDocuments)
                {
                    if (!document.ContainsKey("Categories"))
                    {
                        continue;
                    }

                    foreach (var refDoc in document["Categories"].AsArray)
                    {
                        refDoc["$id"] = categoryIdGuidMap[refDoc["$id"].AsInt32];
                    }
                }

                transactions.Insert(transactionDocuments);

                logger.LogInformation("Migrating Accounts...");

                var accounts = database.GetCollection("AccountDetails");
                var accountDocuments = accounts.FindAll().ToList();
                var accountIbanGuidMap = new Dictionary<string, Guid>();

                foreach (var document in accountDocuments)
                {
                    accounts.Delete(document["_id"]);
                    document["IBAN"] = new BsonValue(document["_id"].AsString);
                    var guid = Guid.NewGuid();
                    accountIbanGuidMap[document["_id"].AsString] = guid;
                    document["_id"] = new BsonValue(guid);
                    document["CreatedAt"] = DateTime.UtcNow;
                    document["UpdatedAt"] = DateTime.UtcNow;
                    document["IsDeleted"] = false;
                    accounts.Insert(document);
                }

                logger.LogInformation("Migrating Balances...");

                var balances = database.GetCollection("Balance");
                var balanceDocuments = balances.FindAll().ToList();

                foreach (var document in balanceDocuments)
                {
                    balances.Delete(document["_id"]);
                    document["_id"] = new BsonValue(Guid.NewGuid());
                    document["CreatedAt"] = DateTime.UtcNow;
                    document["UpdatedAt"] = DateTime.UtcNow;
                    document["IsDeleted"] = false;
                }

                foreach (var document in balanceDocuments.ToList())
                {
                    if (!document.ContainsKey("Account"))
                    {
                        continue;
                    }

                    var oldId = document["Account"].AsDocument["$id"];

                    if (!accountIbanGuidMap.ContainsKey(oldId))
                    {
                        balanceDocuments.Remove(document);
                        continue;
                    }

                    document["Account"]["$id"] = accountIbanGuidMap[oldId];
                }

                balances.Insert(balanceDocuments);

                logger.LogInformation("Migrating online banking connections...");

                var bankingDetails = database.GetCollection("OnlineBankingDetails");
                var bankingDetailsDocuments = bankingDetails.FindAll().ToList();

                foreach (var document in bankingDetailsDocuments)
                {
                    bankingDetails.Delete(document["_id"]);
                    document["_id"] = new BsonValue(Guid.NewGuid());
                    document["CreatedAt"] = DateTime.UtcNow;
                    document["UpdatedAt"] = DateTime.UtcNow;
                    document["IsDeleted"] = false;
                    bankingDetails.Insert(document);
                }

                database.RenameCollection("OnlineBankingDetails", "BankDetails");

                logger.LogInformation("Migration successful");

                database.UserVersion = 1;

                database.Checkpoint();

                logger.LogInformation("Migrated to version 1");
            }
            if (database.UserVersion == 1)
            {
                logger.LogInformation("Database version is 1. Migrating to version 2...");


                logger.LogInformation("Migrating Accounts...");

                var banks = database.GetCollection("BankDetails");
                var bankDocuments = banks.FindAll().ToList();

                var accounts = database.GetCollection("AccountDetails");
                var accountDocuments = accounts.FindAll().ToList();

                foreach (var document in accountDocuments)
                {
                    accounts.Delete(document["_id"]);

                    string bankCode = document["BankCode"];
                    var bank = bankDocuments.FirstOrDefault(d => d["BankCode"].AsInt32.ToString().Equals(bankCode));

                    if (bank != null)
                    {
                        var bankId = bank["_id"];
                        document["Bank"] = new BsonDocument { ["$id"] = bankId, ["$ref"] = "BankDetails" };
                    }

                    document.Remove("BankCode");

                    document["UpdatedAt"] = DateTime.UtcNow;

                    accounts.Insert(document);

                }

                database.RenameCollection("AccountDetails", "Accounts");


                logger.LogInformation("Migrating Categories...");

                var categories = database.GetCollection("Category");
                var categoryDocuments = categories.FindAll().ToList();

                foreach (var document in categoryDocuments)
                {
                    categories.Delete(document["_id"]);

                    var filter = BsonMapper.Global.Deserialize<TransactionFilter>(document["Filter"]);

                    if (filter == null)
                    {
                        continue;
                    }

                    var newFilterDocument = BsonMapper.Global.ToDocument(filter.ToDto());

                    document["Filter"] = newFilterDocument;
                }

                categories.Insert(categoryDocuments);


                logger.LogInformation("Migrating Balances...");

                var balances = database.GetCollection("Balance");
                var balanceDocuments = balances.FindAll().ToList();

                foreach (var document in balanceDocuments)
                {
                    balances.Delete(document["_id"]);
                    document["Account"]["$ref"] = new BsonValue("Accounts");
                }

                balances.Insert(balanceDocuments);


                logger.LogInformation("Migration successful");

                database.UserVersion = 2;

                database.Checkpoint();

                logger.LogInformation("Migrated to version 2");

            }

            if (database.UserVersion == 2)
            {
                logger.LogInformation("Database version is 2. Migrating to version 3...");

                var categories = database.GetCollection("Category");
                var categoryDocuments = categories.FindAll().ToList();
                var categoryIdMap = new Dictionary<Guid, BsonDocument>();

                foreach (var document in categoryDocuments)
                {
                    categoryIdMap.Add(document["_id"], document);
                }


                logger.LogInformation("Migrating Transactions...");

                var transactions = database.GetCollection("Transaction");
                var transactionDocuments = transactions.FindAll().ToList();


                foreach (var document in transactionDocuments)
                {
                    transactions.Delete(document["_id"]);

                    var categoryIds = document["Categories"].AsArray
                        .Select(x => x["$id"])
                        .ToList();

                    if (!categoryIds.Any())
                    {
                        document["Category"] = null;
                        document.Remove("Categories");

                        continue;
                    }

                    var firstWithParent = categoryIds
                        .FirstOrDefault(cid => !categoryIdMap[cid]["Parent"].IsNull);

                    if (firstWithParent is null)
                    {
                        var firstCategoryId = categoryIds.FirstOrDefault();
                        document["Category"] = new BsonDocument { ["$id"] = firstCategoryId, ["$ref"] = "Category" };
                    }
                    else
                    {
                        document["Category"] = new BsonDocument { ["$id"] = firstWithParent, ["$ref"] = "Category" };
                    }

                    document.Remove("Categories");
                    document["UpdatedAt"] = DateTime.UtcNow;
                }

                transactions.Insert(transactionDocuments);


                logger.LogInformation("Migration successful");

                database.UserVersion = 3;

                database.Checkpoint();

                logger.LogInformation("Migrated to version 3");
            }

            if (database.UserVersion == 3)
            {
                logger.LogInformation("Database version is 3. Migrating to version 4...");

                logger.LogInformation("Migrating bank details...");

                var banks = database.GetCollection("BankDetails");
                var bankDocuments = banks.FindAll().ToList();

                foreach (var document in bankDocuments)
                {
                    var bankCode = document["BankCode"].AsInt32;

                    var fintsInstitute = Moneyes.LiveData.BankInstitutes.GetInstitute(bankCode);

                    document["Name"] = fintsInstitute.Name;
                    document["Server"] = fintsInstitute.FinTs_Url;
                    document["HbciVersion"] = fintsInstitute.Version.Contains("3.0") ? 300 : null;
                    document["UpdateAt"] = DateTime.UtcNow;
                }

                banks.Update(bankDocuments);

                logger.LogInformation("Migration successful");

                database.UserVersion = 4;

                database.Checkpoint();

                logger.LogInformation("Migrated to version 4");
            }

            // Drop old indeces
            logger.LogInformation("Dropping old indeces...");

            database.GetCollection("Category").DropIndex("Name");
            database.GetCollection("Transaction").DropIndex("UID");

            logger.LogInformation("Migration successful");

            database.UserVersion = 5;

            database.Checkpoint();
        }

        /// <summary>
        /// Regenerate the UIDs of all transactions an save the changes.
        /// </summary>
        /// <param name="transactionRepository"></param>
        //private static void UpdateUIDs(IUniqueCachedRepository<TransactionDbo> transactionRepository)
        //{
        //    List<TransactionDbo> transactions = transactionRepository.GetAll().ToList();

        //    foreach (TransactionDbo t in transactions)
        //    {
        //        string oldUID = t.UID;
        //        t.RegenerateUID();

        //        if (oldUID != t.UID)
        //        {
        //            if (transactionRepository.DeleteById(oldUID))
        //            {
        //                transactionRepository.Set(t);
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// Removes duplicate transaction entries that equal by a selector but have different UIDs from the repository.
        /// </summary>
        /// <typeparam name="TSelector"></typeparam>
        /// <param name="transactionRepository">The transaction repo.</param>
        /// <param name="uniqueDuplicateIdentifier">The selector used to identify transactions.</param>
        /// <param name="keepOld">An expression used to determine whether to keep the existing transaction.</param>
        /// <param name="delete">An expression used to determine whether this duplicate should be ignored.</param>
        //private static void RemoveDuplicates<T, TSelector>(IBaseRepository<T> transactionRepository,
        //            Func<T, TSelector> uniqueDuplicateIdentifier,
        //            Func<T, bool> keepOld = null,
        //            Func<T, T, bool> delete = null)
        //{
        //    List<T> transactions = transactionRepository.GetAll().ToList();

        //    Dictionary<TSelector, T> done = new();

        //    // Remove duplicates, use duplicate with predicate
        //    foreach (T transaction in transactions)
        //    {
        //        _ = done.TryGetValue(uniqueDuplicateIdentifier(transaction), out T existingTransaction);

        //        if (done.ContainsKey(uniqueDuplicateIdentifier(transaction))
        //            && (delete?.Invoke(transaction, existingTransaction) ?? true))
        //        {
        //            if (keepOld?.Invoke(existingTransaction) ?? true)
        //            {
        //                _ = transactionRepository.DeleteById(IDSelectors.Resolve(transaction));
        //            }
        //            else
        //            {
        //                _ = transactionRepository.DeleteById(IDSelectors.Resolve(existingTransaction));
        //            }

        //            continue;
        //        }
        //        else
        //        {
        //            done[uniqueDuplicateIdentifier(transaction)] = transaction;
        //        }
        //    }
        //}

        private static IEnumerable<T> RemoveDuplicates<T, TSelector>(IEnumerable<T> entities,
            Func<T, TSelector> selector,
            Func<T, T, T> keepSelector)
        {
            Dictionary<TSelector, T> done = new();

            // Remove duplicates, use duplicate with predicate
            foreach (T entity in entities)
            {
                TSelector selectorValue = selector(entity);

                if (done.ContainsKey(selectorValue) && keepSelector != null)
                {
                    done[selectorValue] = keepSelector.Invoke(done[selectorValue], entity);
                }
                else
                {
                    done[selectorValue] = entity;
                }
            }

            return done.Values;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <typeparam name="TSelector"></typeparam>
        ///// <typeparam name="TDeleteSelector"></typeparam>
        ///// <param name="repository"></param>
        ///// <param name="selector"></param>
        ///// <param name="deleteSelector">Unique selector to differentiate even between duplicates (e.g. amount in balance)</param>
        //private static void RemoveDuplicates<T, TSelector, TDeleteSelector>(IBaseRepository<T> repository,
        //    Func<T, TSelector> selector,
        //    Func<T, TDeleteSelector> deleteSelector)
        //{
        //    List<T> entities = repository.GetAll().ToList();
        //    Dictionary<TSelector, T> done = new();
        //    Dictionary<TSelector, List<TDeleteSelector>> duplicates = new();

        //    foreach (T entity in entities)
        //    {
        //        if (done.ContainsKey(selector(entity)))
        //        {
        //            duplicates.TryAdd(selector(entity), new());

        //            duplicates[selector(entity)].Add(deleteSelector(entity));
        //        }
        //        else
        //        {
        //            done[selector(entity)] = entity;
        //        }
        //    }

        //    repository.DeleteMany(x => toDelete.Contains)


        //    return done.Values;
        //}

        private async Task RestoreFromDatabase(string userHome)
        {
            var restorePath = Path.Combine(userHome, @".moneyes_kaputt\database.db");

            var oldDbProvider = new LiteDatabaseProvider(new LiteDbConfig()
            {
                DatabasePath = restorePath,
                CreatePassword = () => string.Empty.ToSecuredString(),
                RequestPassword = () => string.Empty.ToSecuredString(),

            });

            await oldDbProvider.TryOpenDatabase();

            //var oldCategoryRepo = new CategoryRepository(oldDbProvider);
            //var oldTransactionRepo = new TransactionRepository(oldDbProvider);
            //var oldBalanceRepo = new BalanceRepository(oldDbProvider);

            //var oldCategories = oldCategoryRepo.GetAll().ToList();
            //var oldTransactions = oldTransactionRepo.GetAll().ToList();
            //var oldBalances = oldBalanceRepo.GetAll().ToList();
        }

        private MasterPasswordProvider CreateMasterPasswordProvider(IServiceProvider p)
        {
            return new(p.GetRequiredService<IDialogService<InitMasterPasswordDialogViewModel>>(),
                       p.GetRequiredService<IDialogService<GetMasterPasswordDialogViewModel>>(),
                       p.GetRequiredService<InitMasterPasswordDialogViewModel>,
                       p.GetRequiredService<GetMasterPasswordDialogViewModel>);
        }

        private static void RegisterGlobalExceptionHandling(Action<Exception, string> log)
        {
            // this is the line you really want 
            AppDomain.CurrentDomain.UnhandledException +=
                (sender, args) => CurrentDomainOnUnhandledException(args, log);

            // optional: hooking up some more handlers
            // remember that you need to hook up additional handlers when 
            // logging from other dispatchers, shedulers, or applications

            Application.Current.Dispatcher.UnhandledException +=
                (sender, args) => DispatcherOnUnhandledException(args, log);

            Application.Current.DispatcherUnhandledException +=
                (sender, args) => CurrentOnDispatcherUnhandledException(args, log);

            TaskScheduler.UnobservedTaskException +=
                (sender, args) => TaskSchedulerOnUnobservedTaskException(args, log);
        }

        private static void TaskSchedulerOnUnobservedTaskException(UnobservedTaskExceptionEventArgs args, Action<Exception, string> log)
        {
            log(args.Exception, "Unobserved Task Exception");
            args.SetObserved();
        }

        private static void CurrentOnDispatcherUnhandledException(DispatcherUnhandledExceptionEventArgs args, Action<Exception, string> log)
        {
            log(args.Exception, "Dispatcher unhandled exception");
            args.Handled = true;
        }

        private static void DispatcherOnUnhandledException(DispatcherUnhandledExceptionEventArgs args, Action<Exception, string> log)
        {
            log(args.Exception, "Dispatcher unhandled exception");
            args.Handled = true;
        }

        private static void CurrentDomainOnUnhandledException(UnhandledExceptionEventArgs args, Action<Exception, string> log)
        {
            var exception = args.ExceptionObject as Exception;
            var terminatingMessage = args.IsTerminating ? " The application is terminating." : string.Empty;
            var exceptionMessage = exception?.Message ?? "An unmanaged exception occured.";
            var message = string.Concat(exceptionMessage, terminatingMessage);
            log(exception, "Unhandled exception");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            _dbProvider.Database?.Dispose();
        }
    }
}
