using Microsoft.Extensions.Options;
using Moneyes.Core;
using Moneyes.Data;
using Moneyes.LiveData;
using Moneyes.UI.View;
using Moneyes.UI.ViewModels;
using System;
using System.Linq;
using System.Windows;

namespace Moneyes.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        IDisposable _db;
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //var categories = Categories.LoadFromJson();

            var dbFactory = new LiteDbContextFactory(Options.Create<LiteDbConfig>(new() { DatabasePath = "E:\\test.db" }));
            var db = dbFactory.CreateContext();
            var categoryRepo = new CategoryRepository(db);
            var transactionRepo = new TransactionRepository(db);
            var accountRepo = new AccountRepository(db);
            var configStore = new BankConnectionStore(db);

            //var transactionStore = new JsonDatabase<Transaction>("E:\\transcationsTest.json", transaction => transaction.GetUID());
            //var accountStore = new JsonDatabase<AccountDetails>("E:\\accountTest.json", account => account.IBAN);
            //var categoryStore = new CategoryDatabase("categories.json");

            //foreach (var c in await categoryStore.GetAll())
            //{
            //    categoryRepo.Set(c);
            //}

            var passwordPrompt = new DialogPasswordPrompt();

            LiveDataService liveDataService = new(
                transactionRepo, categoryRepo, accountRepo, 
                configStore, new OnlineBankingServiceFactory(), passwordPrompt);

            ExpenseIncomServieUsingDb expenseIncomeService = new(categoryRepo, transactionRepo);
            //TransactionService transactionService = new(transactionRepo);

            var mainViewModel = new MainViewModel(liveDataService, expenseIncomeService, transactionRepo,
                accountRepo, configStore);
            
            var settingsViewModel = new BankingSettingsViewModel(liveDataService, configStore);

            var mainWindowViewModel = new MainWindowViewModel()
            {
                Tabs = new() { mainViewModel, settingsViewModel },
                CurrentViewModel = mainViewModel
            };

            if (!configStore.HasBankingDetails)
            {
                mainWindowViewModel.CurrentViewModel = settingsViewModel;
            }

            this.MainWindow = new MainWindow(mainWindowViewModel);
            this.MainWindow.Show();
            _db = db;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            _db.Dispose();
        }
    }
}
