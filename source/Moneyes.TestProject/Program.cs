using MoneyesParser;
using MoneyesParser.Filters;
using MoneyesParser.Parsing;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var categories = Categories.LoadFromJson();
            var sales = SalesParser.Parse("Test files/1year.csv");

            var expenseSales = sales.Where(sale => sale.SaleType == SaleType.Expense);

            var filteredByCategory = categories.ToList().Select(category =>
            {
                var filtered = expenseSales.FilterSales(category.Filter);

                return (category, filtered);
            });

            var restSales = filteredByCategory.Aggregate(expenseSales, (sales, pair) => sales.Except(pair.filtered));

            //var supermarketExpenses = expenseSales.FilterSales(categories.Single(c => c.Name == "Supermarket").Filter);
            //var housingExpenses = expenseSales.FilterSales(categories.Single(c => c.Name == "Housing").Filter);
            //var onlineShoppingExpenses = expenseSales.FilterSales(categories.Single(c => c.Name == "Online shopping").Filter);

            //var restSales = expenseSales
            //    .Except(supermarketExpenses)
            //    .Except(housingExpenses)
            //    .Except(onlineShoppingExpenses);

            //PrintSales("Supermarket", supermarketExpenses, expenseSales);
            //PrintSales("Housing", housingExpenses, expenseSales);
            //PrintSales("Online shopping", onlineShoppingExpenses, expenseSales);

            PrintSales("Total", expenseSales, printFull: false);
            filteredByCategory.ToList().ForEach(c => PrintSales(c.category.Name, c.filtered, expenseSales));
            PrintSales("Other", restSales, expenseSales);
        }

        static void PrintSales(string category, IEnumerable<ISale> sales,
            IEnumerable<ISale> unfilteredSales = null, bool printFull = true)
        {
            var startDate = (unfilteredSales ?? sales).FindStartDate();
            var endDate = (unfilteredSales ?? sales).FindEndDate();
            var totalDays = ((int)(endDate - startDate).TotalDays + 1);

            var (total, avg) = sales.CalulateTotalAndAverageAmount(totalDays);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{category} expenses from {startDate:d} to {endDate:d} ({totalDays} days):");
            Console.WriteLine($"Total: {total:C}");
            Console.WriteLine($"Avg ({30} days): {avg:C}");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();

            if (printFull)
            {
                foreach (var sale in sales)
                {
                    Console.WriteLine(sale);
                }

                Console.WriteLine();
            }
        }

        static void PrintFiltered(string category, IEnumerable<ISale> sales, SalesFilter filter)
        {
            var filteredSales = sales.FilterSales(filter);

            PrintSales(category, filteredSales, sales);
        }
    }
}
