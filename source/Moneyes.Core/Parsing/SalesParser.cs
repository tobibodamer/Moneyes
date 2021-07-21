﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;

namespace MoneyesParser.Parsing
{
    /// <summary>
    /// A parser used to extract <see cref="ISale"/>s from a <c>.csv</c> file.
    /// </summary>
    public class SalesParser
    {
        /// <summary>
        /// Parses the sales from a <c>.csv</c> file, formatted with the default banking format.
        /// </summary>
        /// <param name="fileName">The path to the <c>.csv</c> file.</param>
        /// <param name="delimiter">The delimiter to use.</param>
        /// <returns>A collection of parsed <see cref="ISale"/>s.</returns>
        public static IEnumerable<ISale> Parse(string fileName, string delimiter = ";")
        {
            var config = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                Delimiter = delimiter
            };

            using var reader = new StreamReader(fileName);
            using var csv = new CsvReader(reader, config);

            var entries = csv.GetRecords<SaleEntry>().ToList();
            var sales = CreateSales(entries);

            return sales;
        }
        
        static IEnumerable<ISale> CreateSales(IEnumerable<SaleEntry> entries)
        {
            foreach (var entry in entries)
            {
                yield return CreateSale(entry);
            }
        }

        static ISale CreateSale(SaleEntry entry)
        {
            // Parse date
            var formattedDate = entry.Verwendungszweck.Split(' ').First();

            DateTime.TryParseExact(formattedDate, "yyyy-MM-ddTHH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date);

            // Parse location and name

            Regex rx = new (@"\/\/([A-Za-z\s.\-]+)\/([A-Z][A-Z])");
            Match match = rx.Match(entry.Beguenstigter);
            string country = null;
            string city = null;
            string name = entry.Beguenstigter;

            if (match.Success)
            {
                if (match.Groups.Count > 0 && match.Groups[1].Success)
                {
                    city = match.Groups[1].Value;

                    if (match.Groups.Count > 1 && match.Groups[1].Success)
                    {
                        country = match.Groups[2].Value;
                    }
                }

                name = entry.Beguenstigter.Replace(match.Groups[0].Value, "");
            }


            ISale s = new Sale
            {
                PaymentDate = date,
                BookingDate = entry.Buchungstag,
                City = city,
                Country = country,
                Amount = Math.Abs(entry.Betrag),
                Name = name,
                BookingType = entry.Buchungstext,
                SaleType = entry.Betrag > 0 ? SaleType.Income : SaleType.Expense
            };

            return s;
        }
    }


}
