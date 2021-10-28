using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moneyes.Core;
using Moneyes.Data;
using Moneyes.LiveData;
using Moneyes.UI.View;
using Moneyes.UI.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Windows;

namespace Moneyes.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        ILiteDatabase _db;

        private static async Task<Result<ILiteDatabase>> TryOpenDatabase(LiteDbConfig databaseConfig)
        {
            LiteDbContextFactory databaseFactory = new(databaseConfig);

            // Input password
            DialogPasswordPrompt passwordPrompt = new("Password required", "Please enter your master password.");
            SecureString password;
            ILiteDatabase database;

            // Database exists -> Try open database without master password
            if (File.Exists(databaseConfig.DatabasePath))
            {
                try
                {
                    database = databaseFactory.CreateContext();
                    return Result.Successful(database);
                }
                catch (LiteException)
                {
                    // Failed to open without master password
                }
            }

            // No password failed, or database doesn't exist -> Try with master password
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    password = await passwordPrompt.WaitForPasswordAsync();

                    database = databaseFactory
                        .CreateContext(password.ToUnsecuredString());

                    return Result.Successful(database);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Could not open database", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }


            // Opening database failed
            return Result.Failed<ILiteDatabase>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //var categories = Categories.LoadFromJson();

            IServiceCollection services = new ServiceCollection();

            // Load database

            string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string dataDir = Path.Combine(userHome, ".moneyes");
            Directory.CreateDirectory(dataDir);

            LiteDbConfig databaseConfig = new()
            {
                DatabasePath = Path.Combine(dataDir, "database.db")
            };

            Result<ILiteDatabase> databaseResult = await TryOpenDatabase(databaseConfig);

            if (!databaseResult.IsSuccessful)
            {
                // Database opening / creation failed
                Environment.Exit(-1);
            }

            _db = databaseResult.Data;

            // Register database
            services.AddSingleton<ILiteDatabase>(_db);

            services.AddScoped<CategoryRepository>();
            services.AddScoped<IBaseRepository<Category>, CategoryRepository>(p => p.GetRequiredService<CategoryRepository>());
            services.AddScoped<TransactionRepository>();
            services.AddScoped<IBaseRepository<Transaction>, TransactionRepository>(p => p.GetRequiredService<TransactionRepository>());
            services.AddScoped<AccountRepository>();
            services.AddScoped<IBaseRepository<AccountDetails>, AccountRepository>(p => p.GetRequiredService<AccountRepository>());
            services.AddScoped<BalanceRepository>();
            services.AddScoped<IBaseRepository<Balance>, BalanceRepository>(p => p.GetRequiredService<BalanceRepository>());
            services.AddScoped<BankConnectionStore>();


            //var categoryRepo = new CategoryRepository(database);
            //var transactionRepo = new TransactionRepository(database);
            //var accountRepo = new AccountRepository(database);
            //var configStore = new BankConnectionStore(database);



            //var transactionStore = new JsonDatabase<Transaction>("E:\\transcationsTest.json", transaction => transaction.GetUID());
            //var accountStore = new JsonDatabase<AccountDetails>("E:\\accountTest.json", account => account.IBAN);



            services.AddTransient<IPasswordPrompt, DialogPasswordPrompt>(p => 
                new DialogPasswordPrompt("Password required", "Enter your online banking password / PIN:"));
            services.AddSingleton<OnlineBankingServiceFactory>();

            services.AddScoped<LiveDataService>();
            services.AddScoped<IExpenseIncomeService, ExpenseIncomServieUsingDb>();
            services.AddScoped<IBankingService, BankingService>();
            services.AddScoped<ITransactionService, TransactionService>();

            services.AddTransient<ITabViewModel, MainViewModel>();
            services.AddTransient<ITabViewModel, BankingSettingsViewModel>();
            services.AddTransient<MainWindowViewModel>();
            //passwordPrompt = new DialogPasswordPrompt();

            //LiveDataService liveDataService = new(
            //    transactionRepo, categoryRepo, accountRepo,
            //    configStore, new OnlineBankingServiceFactory(), passwordPrompt);

            //ExpenseIncomServieUsingDb expenseIncomeService = new(categoryRepo, transactionRepo);
            //TransactionService transactionService = new(transactionRepo);

            //var mainViewModel = new MainViewModel(liveDataService, expenseIncomeService, transactionRepo,
            //    accountRepo, configStore);

            //var settingsViewModel = new BankingSettingsViewModel(liveDataService, configStore);

            //var mainWindowViewModel = new MainWindowViewModel()
            //{
            //    Tabs = new() { mainViewModel, settingsViewModel },
            //    CurrentViewModel = mainViewModel
            //};

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            var categoryRepo = serviceProvider.GetService<CategoryRepository>();
            var categoryStore = new CategoryDatabase("categories.json");

            foreach (var c in await categoryStore.GetAll())
            {
                if (categoryRepo.FindByName(c.Name) == null)
                {
                    categoryRepo.Set(c);
                }
            }

            MainWindowViewModel mainWindowViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();

            this.MainWindow = new MainWindow(mainWindowViewModel);
            this.MainWindow.Show();

            this.MainWindow.Closed += (s, e) =>
            {
                Environment.Exit(0);
            };
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            _db.Dispose();
        }
    }
}
