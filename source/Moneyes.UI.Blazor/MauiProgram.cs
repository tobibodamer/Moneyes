using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Logging;
using Moneyes.Core.Filters;
using Moneyes.UI.Blazor.Data;
using MudBlazor.Services;
using static MudBlazor.Colors;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.Json;
using Moneyes.Data;
using Moneyes.LiveData;
using LiteDB;
using Moneyes.UI;
using Serilog;
using Moneyes.UI.Blazor.Stores;

namespace Moneyes.UI.Blazor
{
    public static class MauiProgram
    {
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
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMudServices();

            builder.Services.AddMauiBlazorWebView();
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
#endif

            // Database
            builder.Services.AddLiteDb<UIDatabaseProvider>(config =>
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
            builder.Services.AddTransient<ICategoryFactory, CategoryFactory>();
            builder.Services.AddTransient<ITransactionFactory, TransactionFactory>();
            builder.Services.AddTransient<IFilterFactory, FilterFactory>();
            builder.Services.AddTransient<IAccountFactory, AccountFactory>();
            builder.Services.AddTransient<IBalanceFactory, BalanceFactory>();
            builder.Services.AddTransient<IBankDetailsFactory, BankDetailsFactory>();
            
            // Services

            builder.Services.AddTransient<IPasswordPrompt, MauiPasswordPrompt>();
            builder.Services.AddSingleton<IOnlineBankingServiceFactory, OnlineBankingServiceFactory>();
            builder.Services.AddSingleton<IStatusMessageService, MauiStatusMessageService>();
            builder.Services.AddSingleton<MasterPasswordProvider>(CreateMasterPasswordProvider);

            builder.Services.AddScoped<LiveDataService>();
            builder.Services.AddScoped<IExpenseIncomeService, ExpenseIncomServiceUsingDb>();
            builder.Services.AddScoped<IBankingService, BankingService>();
            builder.Services.AddScoped<ITransactionService, TransactionService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<IMauiInitializeService, MauiDatabaseInitializer>();


            SetupLogging();
            builder.Services.AddLogging(builder => builder.AddSerilog());


            builder.Services.AddSingleton<TransactionStore>();

            return builder.Build();
        }

        private static string InitDatabasePath()
        {
            string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string dataDir = Path.Combine(userHome, ".moneyes");

            Directory.CreateDirectory(dataDir);

            return Path.Combine(dataDir, "database.db");
        }

        private static MasterPasswordProvider CreateMasterPasswordProvider(IServiceProvider p)
        {
            return new();
        }
    }
}