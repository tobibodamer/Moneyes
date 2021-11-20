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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Moneyes.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IDatabaseProvider _dbProvider;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            RegisterGlobalExceptionHandling((ex, msg) =>
            {
                Log.Logger.Error(ex, msg);
            });

            IDSelectors.Register<Category>(c => c.Id);
            IDSelectors.Register<AccountDetails>(acc => acc.IBAN);
            IDSelectors.Register<Transaction>(t => t.UID);

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logDir = Path.Combine(appData, @"Moneyes\logs");
            
            Directory.CreateDirectory(logDir);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Debug()
                .WriteTo.File(path: Path.Combine(logDir, "log.txt"), rollingInterval: RollingInterval.Day)
                .MinimumLevel.Debug()
                .CreateLogger();


            IServiceCollection services = new ServiceCollection();

            // Create DB config

            string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string dataDir = Path.Combine(userHome, ".moneyes");
            Directory.CreateDirectory(dataDir);

            LiteDbConfig databaseConfig = new()
            {
                DatabasePath = Path.Combine(dataDir, "database.db")
            };

            services.AddSingleton<LiteDbConfig>(databaseConfig);
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
            services.AddTransient<ITabViewModel, TransactionsViewModel>();
            services.AddTransient<ITabViewModel, AccountsViewModel>();
            services.AddTransient<ITabViewModel, BankSetupViewModel>();

            services.AddTransient<SetupWizardViewModel>();

            services.AddScoped<CategoryViewModelFactory>();
            services.AddTransient<ExpenseCategoriesViewModel>();
            services.AddTransient<SelectorViewModel>();

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

            
            
            

            //var newTransactions = transactionRepo.GetAll();

            //categoryRepo.Set(oldCategories);

            //var newCategories = categoryRepo.GetAll();


            //var csv = CsvParser.FromMT940CSV("1.csv").ToList();

            //var categoryStore = new CategoryDatabase("categories.json");

            //foreach (var c in await categoryStore.GetAll())
            //{
            //    if (categoryRepo.FindByName(c.Name) == null)
            //    {
            //        categoryRepo.Set(c);
            //    }
            //}

            MainWindowViewModel mainWindowViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();

            this.MainWindow = new MainWindow(mainWindowViewModel);
            this.MainWindow.Show();

            this.MainWindow.Closed += (s, e) =>
            {
                Environment.Exit(0);
            };
        }

        private IEnumerable<Transaction> RemoveDuplicates(IEnumerable<Transaction> transactions,
            Func<Transaction, bool> selectDupeOverExisting)
        {
            Dictionary<string, Transaction> done = new();

            // Remove duplicates, use duplicate with predicate
            foreach (var oldTransaction in transactions)
            {
                if (done.ContainsKey(oldTransaction.UID) && 
                    (selectDupeOverExisting?.Invoke(done[oldTransaction.UID]) ?? true))
                {
                    continue;
                }
                else
                {
                    done[oldTransaction.UID] = oldTransaction;
                }
            }

            return done.Values;
        }

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
