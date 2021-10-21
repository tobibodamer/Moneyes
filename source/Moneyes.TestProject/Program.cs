using libfintx.FinTS;
using libfintx.FinTS.Data;
using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Core.Parsing;
using Moneyes.LiveData;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Test
{
    

    class Program
    {

        //static async Task<IEnumerable<Moneyes.Core.Transaction>> GetLiveTransactions()
        //{
        //    var details = new OnlineBankingDetails()
        //    {
        //        BankCode = 66650085,
        //        Pin = "***REMOVED***",
        //        UserId = "***REMOVED***"
        //    };

        //    OnlineBankingService onlineBanking = new OnlineBankingServiceFactory(null)
        //        .CreateService(details);

        //    var result = await onlineBanking.Transactions(startDate: new DateTime(2021, 10, 1));

        //    if (!result.IsSuccessful)
        //    {
        //        return Enumerable.Empty<Moneyes.Core.Transaction>();
        //    }

        //    return result.Data;
        //}

        static async Task AsyncMain()
        {
            //TransactionStore store = new("E:\\transcationsTest.json");

            //var liveTransactions = await GetLiveTransactions();

            //await store.Store(liveTransactions);

            //var csvTransactions = CsvParser.FromFile("Test files/1month.csv").ToList();

            //await store.Store(csvTransactions);

            //var sparkasseService = new Moneyes.LiveData.SparkasseSalesService();

            //await sparkasseService.Login("***REMOVED***", "***REMOVED***");

            //string csvContent = await sparkasseService.GetSalesCsvContent(accounts =>
            //    accounts.First(), new (DateTime.Now.Year, DateTime.Now.Month, 1), DateTime.Now);

            //await sparkasseService.Logout();

            Console.OutputEncoding = System.Text.Encoding.UTF8;

            //var categories = Categories.LoadFromJson();
            //var sales = SalesParser.Parse("Test files/1month.csv");
            ////var liveSales = SalesParser.ParseFromContent(csvContent);

            //var expenseSales = sales.Where(sale => sale.SaleType == SaleType.Expense);

            //var filteredByCategory = categories.ToList().Select(category =>
            //{
            //    var filtered = expenseSales.FilterSales(category.Filter);

            //    return (category, filtered);
            //});

            //var restSales = filteredByCategory.Aggregate(expenseSales, (sales, pair) => sales.Except(pair.filtered));

            //foreach (var (category, filtered) in filteredByCategory)
            //{
            //    var sum = filtered.Sum(sale => sale.Amount);

            //    if (sum == 0) { continue; }

            //    Console.WriteLine($"{category.Name,20}: {sum,7}  €");
            //}

            //var restAmt = restSales.Sum(sale => sale.Amount);

            //Console.WriteLine($"{"Other",20}: {restAmt,7}  €");

            //var totalAmt = expenseSales.Sum(sale => sale.Amount);

            //Console.WriteLine("-".PadRight(50, '-'));
            //Console.WriteLine($"{"Total",20}: {totalAmt,7}  €");

            //Console.ReadLine();

            //// sort into categories

            //var sortedIntoCategories = sales.Select(sale => (sale, categories.Where(c => c.Filter.Evaluate(sale))));


            //Dictionary<Category, List<ISale>> categorySalesMap = new();
            //Dictionary<ISale, List<Category>> saleCategoryMap = new();

            //foreach (var category in categories)
            //{
            //    categorySalesMap.Add(category, new());

            //    foreach (var sale in sales)
            //    {
            //        if (category.Filter.Evaluate(sale))
            //        {
            //            if (!saleCategoryMap.TryGetValue(sale, out var list))
            //            {
            //                saleCategoryMap.Add(sale, list = new());
            //            }

            //            saleCategoryMap[sale].Add(category);
            //            categorySalesMap[category].Add(sale);
            //        }
            //    }
            //}

            //PrintSales("Total", expenseSales, printFull: false);
            //filteredByCategory.ToList().ForEach(c => PrintSales(c.category.Name, c.filtered, expenseSales, true));
            //PrintSales("Other", restSales, expenseSales);
        }

        public static void Main(string[] args)
        {
            AsyncMain().GetAwaiter().GetResult();
        }

        //static void PrintSales(string category, IEnumerable<ISale> sales,
        //    IEnumerable<ISale> unfilteredSales = null, bool printFull = true)
        //{
        //    var startDate = (unfilteredSales ?? sales).FindStartDate();
        //    var endDate = (unfilteredSales ?? sales).FindEndDate();
        //    var totalDays = ((int)(endDate - startDate).TotalDays + 1);

        //    var (total, avg) = sales.CalulateTotalAndAverageAmount(totalDays);

        //    Console.ForegroundColor = ConsoleColor.Green;
        //    Console.WriteLine($"{category} expenses from {startDate:d} to {endDate:d} ({totalDays} days):");
        //    Console.WriteLine($"Total: {total:C}");
        //    Console.WriteLine($"Avg ({30} days): {avg:C}");

        //    Console.ForegroundColor = ConsoleColor.White;
        //    Console.WriteLine();

        //    if (printFull)
        //    {
        //        foreach (var sale in sales)
        //        {
        //            Console.WriteLine(sale);
        //        }

        //        Console.WriteLine();
        //    }
        //}

        //static void PrintFiltered(string category, IEnumerable<ISale> sales, TransactionFilter filter)
        //{
        //    var filteredSales = sales.FilterSales(filter);

        //    PrintSales(category, filteredSales, sales);
        //}
    }
}
