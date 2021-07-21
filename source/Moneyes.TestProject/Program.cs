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
            var sales = SalesParser.Parse("20210625-7888600-umsatz.csv");
            
            var expenseSales = sales.Where(sale => sale.SaleType == SaleType.Expense);

            var supermarketExpenses = expenseSales.FilterSales(categories.Single(c => c.Name == "Supermarket").Filter);
            var housingExpenses = expenseSales.FilterSales(categories.Single(c => c.Name == "Housing").Filter);
            var onlineShoppingExpenses = expenseSales.FilterSales(categories.Single(c => c.Name == "Online shopping").Filter);

            var restSales = expenseSales
                .Except(supermarketExpenses)
                .Except(housingExpenses)
                .Except(onlineShoppingExpenses);

            PrintSales("Supermarket", supermarketExpenses);
            PrintSales("Housing", housingExpenses);
            PrintSales("Online shopping", onlineShoppingExpenses);
            PrintSales("Other", restSales);
        }

        static void PrintSales(string category, IEnumerable<ISale> sales)
        {
            var (total, avg) = sales.CalulateTotalAndAverageAmount();

            var startDate = sales.FindStartDate();
            var endDate = sales.FindEndDate();
            var totalDays = ((int)(endDate - startDate).TotalDays);

            Console.WriteLine($"{category} expenses from {startDate:d} to {endDate:d} ({totalDays} days):");
            Console.WriteLine($"Total: {total:C}");
            Console.WriteLine($"Avg ({30} days): {avg:C}");
            Console.WriteLine();

            foreach (var sale in sales)
            {
                Console.WriteLine(sale);
            }

            Console.WriteLine();
        }

        static void PrintFiltered(string category, IEnumerable<ISale> sales, SalesFilter filter)
        {
            var filteredSales = sales.FilterSales(filter);

            PrintSales(category, filteredSales);
        }
    }
}
