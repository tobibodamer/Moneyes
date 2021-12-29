using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moneyes.Core;
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
        private IDatabaseProvider _dbProvider;

        private static void InitializeCultures()
        {
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(
                XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
        }

        private static void RegisterIdSelectors()
        {
            IDSelectors.Register<Category>(c => c.Id);
            IDSelectors.Register<AccountDetails>(acc => acc.IBAN);
            IDSelectors.Register<Transaction>(t => t.UID);
            IDSelectors.Register<Balance>(b => b.UID);
        }

        private static LiteDbConfig CreateDbConfiguration()
        {
            string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string dataDir = Path.Combine(userHome, ".moneyes");
            Directory.CreateDirectory(dataDir);

            LiteDbConfig databaseConfig = new()
            {
                DatabasePath = Path.Combine(dataDir, "database.db")
            };

            return databaseConfig;
        }

        private static void SetupLogging()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logDir = Path.Combine(appData, @"Moneyes\logs");

            Directory.CreateDirectory(logDir);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Debug()
                .WriteTo.File(path: Path.Combine(logDir, "log.txt"), rollingInterval: RollingInterval.Day)
                .MinimumLevel.Debug()
                .CreateLogger();
        }
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            RegisterGlobalExceptionHandling((ex, msg) =>
            {
                Log.Logger.Error(ex, msg);
            });

            InitializeCultures();

            RegisterIdSelectors();

            SetupLogging();

            IServiceCollection services = new ServiceCollection();

            // Database
            services.AddSingleton<LiteDbConfig>(CreateDbConfiguration());
            services.AddSingleton<IDatabaseProvider, DatabaseProvider>(CreateDatabaseProvider);


            // Repositories
            services.AddScoped<CategoryRepository>();
            services.AddScoped<IBaseRepository<Category>, CategoryRepository>(p => p.GetRequiredService<CategoryRepository>());
            services.AddScoped<TransactionRepository>();
            services.AddScoped<IBaseRepository<Transaction>, TransactionRepository>(p => p.GetRequiredService<TransactionRepository>());
            services.AddScoped<AccountRepository>();
            services.AddScoped<IBaseRepository<AccountDetails>, AccountRepository>(p => p.GetRequiredService<AccountRepository>());
            services.AddScoped<BalanceRepository>();
            services.AddScoped<IBaseRepository<Balance>, BalanceRepository>(p => p.GetRequiredService<BalanceRepository>());
            services.AddScoped<IBankConnectionStore, BankConnectionStore>();

            // Services

            services.AddTransient<IPasswordPrompt, OnlineBankingPasswordPrompt>();
            services.AddSingleton<IOnlineBankingServiceFactory, OnlineBankingServiceFactory>();

            services.AddScoped<LiveDataService>();
            services.AddScoped<IExpenseIncomeService, ExpenseIncomServieUsingDb>();
            services.AddScoped<IBankingService, BankingService>();
            //services.AddScoped<ITransactionService, TransactionService>();
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
            services.AddTransient<ITabViewModel, AddressBookViewModel>();
            services.AddTransient<ITabViewModel, AccountsViewModel>();
            services.AddTransient<ITabViewModel, BankSetupViewModel>();

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
            _dbProvider = serviceProvider.GetRequiredService<IDatabaseProvider>();

            if (!_dbProvider.IsDatabaseCreated)
            {
                if (!_dbProvider.TryCreateDatabase())
                {
                    Environment.Exit(-1);
                }
            }
            else
            {
                if (!_dbProvider.TryOpenDatabase())
                {
                    Environment.Exit(-1);
                }
            }


            var categoryRepo = serviceProvider.GetService<CategoryRepository>();
            var transactionRepo = serviceProvider.GetService<TransactionRepository>();
            var balanceRepo = serviceProvider.GetService<BalanceRepository>();

            // Preload caches
            categoryRepo.UpdateCache();
            transactionRepo.UpdateCache();

            UpdateUIDs(transactionRepo);

            RemoveDuplicates(
                transactionRepo,
                t => t.BookingDate.ToString() + t.BookingType + t.Name + t.Amount + t.Index + t.Purpose,
                t => t.PartnerIBAN != null, (t1, t2) => t1.PartnerIBAN == null ^ t2.PartnerIBAN == null);

            RemoveDuplicates(
                transactionRepo,
                t => t.BookingDate.ToString() + t.BookingType + t.Name + t.Amount + t.Index + t.PartnerIBAN,
                t => t.Purpose != null, (t1, t2) => t1.Purpose == null ^ t2.Purpose == null);

            //List<Transaction> csv = CsvParser.FromMT940CSV("viele.csv").ToList();


            //var categoryService = serviceProvider.GetService<ICategoryService>();
            //categoryService.AssignCategories(csv, updateDatabase: true);
            //transactionRepo.Set(csv);

            //var categoryStore = new CategoryDatabase("categories.json");

            //foreach (var c in await categoryStore.GetAll())
            //{
            //    if (categoryRepo.FindByName(c.Name) == null)
            //    {
            //        categoryRepo.Set(c);
            //    }
            //}

            //var balances = balanceRepo.GetAll().ToList();

            //var noDuplicates = RemoveDuplicates(balances, b => b.UID,
            //    (existing, duplicate) => existing.Amount > duplicate.Amount ? existing : duplicate);

            //_ = balanceRepo.DeleteAll();

            //balanceRepo.Set(noDuplicates);

            MainWindowViewModel mainWindowViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();

            this.MainWindow = new MainWindow(mainWindowViewModel);
            this.MainWindow.Show();

            this.MainWindow.Closed += (s, e) =>
            {
                Environment.Exit(0);
            };
        }

        /// <summary>
        /// Regenerate the UIDs of all transactions an save the changes.
        /// </summary>
        /// <param name="transactionRepository"></param>
        private static void UpdateUIDs(TransactionRepository transactionRepository)
        {
            List<Transaction> transactions = transactionRepository.GetAll().ToList();

            foreach (Transaction t in transactions)
            {
                string oldUID = t.UID;
                t.RegenerateUID();

                if (oldUID != t.UID)
                {
                    if (transactionRepository.Delete(oldUID))
                    {
                        transactionRepository.Set(t);
                    }
                }
            }
        }

        /// <summary>
        /// Removes duplicate transaction entries that equal by a selector but have different UIDs from the repository.
        /// </summary>
        /// <typeparam name="TSelector"></typeparam>
        /// <param name="transactionRepository">The transaction repo.</param>
        /// <param name="uniqueDuplicateIdentifier">The selector used to identify transactions.</param>
        /// <param name="keepOld">An expression used to determine whether to keep the existing transaction.</param>
        /// <param name="delete">An expression used to determine whether this duplicate should be ignored.</param>
        private static void RemoveDuplicates<T, TSelector>(IBaseRepository<T> transactionRepository,
            Func<T, TSelector> uniqueDuplicateIdentifier,
            Func<T, bool> keepOld = null,
            Func<T, T, bool> delete = null)
        {
            List<T> transactions = transactionRepository.GetAll().ToList();

            Dictionary<TSelector, T> done = new();

            // Remove duplicates, use duplicate with predicate
            foreach (T transaction in transactions)
            {
                _ = done.TryGetValue(uniqueDuplicateIdentifier(transaction), out T existingTransaction);

                if (done.ContainsKey(uniqueDuplicateIdentifier(transaction))
                    && (delete?.Invoke(transaction, existingTransaction) ?? true))
                {
                    if (keepOld?.Invoke(existingTransaction) ?? true)
                    {
                        _ = transactionRepository.Delete(IDSelectors.Resolve(transaction));
                    }
                    else
                    {
                        _ = transactionRepository.Delete(IDSelectors.Resolve(existingTransaction));
                    }

                    continue;
                }
                else
                {
                    done[uniqueDuplicateIdentifier(transaction)] = transaction;
                }
            }
        }

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

        private void RestoreFromDatabase(string userHome)
        {
            var restorePath = Path.Combine(userHome, @".moneyes_kaputt\database.db");

            var restoreConfig = new LiteDbConfig() { DatabasePath = restorePath };

            var oldDbProvider = new DatabaseProvider(() => string.Empty.ToSecuredString(), () => string.Empty.ToSecuredString(),
                                    restoreConfig);

            oldDbProvider.TryOpenDatabase();

            var oldCategoryRepo = new CategoryRepository(oldDbProvider);
            var oldTransactionRepo = new TransactionRepository(oldDbProvider);
            var oldBalanceRepo = new BalanceRepository(oldDbProvider);

            var oldCategories = oldCategoryRepo.GetAll().ToList();
            var oldTransactions = oldTransactionRepo.GetAll().ToList();
            var oldBalances = oldBalanceRepo.GetAll().ToList();
        }

        private MasterPasswordProvider CreateMasterPasswordProvider(IServiceProvider p)
        {
            return new(p.GetRequiredService<IDialogService<InitMasterPasswordDialogViewModel>>(),
                       p.GetRequiredService<IDialogService<GetMasterPasswordDialogViewModel>>(),
                       p.GetRequiredService<InitMasterPasswordDialogViewModel>,
                       p.GetRequiredService<GetMasterPasswordDialogViewModel>);
        }

        private DatabaseProvider CreateDatabaseProvider(IServiceProvider p)
        {
            var masterPasswordProvider = p.GetRequiredService<MasterPasswordProvider>();
            var dbConfig = p.GetRequiredService<LiteDbConfig>();

            return new(
                masterPasswordProvider.CreateMasterPassword,
                masterPasswordProvider.RequestMasterPassword,
                dbConfig);
        }

        private void RegisterGlobalExceptionHandling(Action<Exception, string> log)
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
            _dbProvider.Database.Dispose();
        }
    }
}
