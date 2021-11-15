using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;

namespace Moneyes.Core.Parsing
{
    /// <summary>
    /// A parser used to extract <see cref="Transaction"/>s from <c>csv</c> formatted data.
    /// </summary>
    public class CsvParser
    {
        /// <summary>
        /// Parses the transaction from a <c>.csv</c> file, formatted with the default banking format.
        /// </summary>
        /// <param name="fileName">The path to the <c>.csv</c> file.</param>
        /// <param name="delimiter">The delimiter to use.</param>
        /// <returns>A collection of parsed <see cref="Transaction"/>s.</returns>
        public static IEnumerable<Transaction> FromFile(string fileName, string delimiter = ";")
        {
            using var reader = new StreamReader(fileName);
            return ParseInternal(reader, delimiter);
        }

        public static IEnumerable<Transaction> FromMT940CSV(string fileName, string delimiter = ";")
        {
            using var reader = new StreamReader(fileName, System.Text.Encoding.GetEncoding("iso-8859-1"));
            
            return ParseInternalMT940(reader, delimiter);
        }

        /// <summary>
        /// Parses the sales from <c>csv</c> content directly, formatted with the default banking format.
        /// </summary>
        /// <param name="content">The <c>csv</c> content.</param>
        /// <param name="delimiter">The delimiter to use.</param>
        /// <returns>A collection of parsed <see cref="Transaction"/>s.</returns>
        public static IEnumerable<Transaction> FromContent(string content, string delimiter = ";")
        {
            using var reader = new StringReader(content);
            return ParseInternal(reader, delimiter);
        }

        private static IEnumerable<Transaction> ParseInternal(TextReader reader, string delimiter)
        {
            var config = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                Delimiter = delimiter,
                Encoding = System.Text.Encoding.UTF8
            };

            using var csv = new CsvReader(reader, config);

            var transactions = csv.GetRecords<TransactionCsvEntry>().ToList()
                .Select(record => FromCsvTransaction(record));

            return transactions;
        }

        private static IEnumerable<Transaction> ParseInternalMT940(TextReader reader, string delimiter)
        {
            var config = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                Delimiter = delimiter,
                Encoding = System.Text.Encoding.Default                
            };
            
            using var csv = new CsvReader(reader, config);

            csv.Context.TypeConverterOptionsCache.GetOptions<string>().NullValues.Add("");

            var transactions = csv.GetRecords<TransactionMT940CsvEntry>().ToList()
                .Select(record => FromMT940CsvTransaction(record));

            return transactions;
        }

        private static Transaction FromCsvTransaction(TransactionCsvEntry entry)
        {
            string shortPartnerName = entry.AccountName;

            if (entry.AccountName.Contains("//"))
            {
                 shortPartnerName = entry.AccountName.Split("//").First().Trim();
            }

            return new()
            {
                Amount = entry.Amount,
                AltName = entry.AccountName,
                Name = shortPartnerName,
                IBAN = entry.Auftragskonto,
                BIC = entry.BIC,
                Purpose = entry.Verwendungszweck,
                BookingType = entry.Buchungstext,
                BookingDate = entry.Buchungstag,
                ValueDate = entry.Valutadatum,
                Currency = entry.Waehrung,
                PartnerIBAN = entry.IBAN
            };
        }

        private static Transaction FromMT940CsvTransaction(TransactionMT940CsvEntry entry)
        {
            var purposes = ParseMT940Purposes(entry.Verwendungszweck);

            string purpose = purposes.GetValueOrDefault("SVWZ");
            string altName = purposes.GetValueOrDefault("ABWA");

            return new()
            {
                Amount = entry.Amount,
                AltName = altName,
                Name = entry.AccountName,
                IBAN = entry.Auftragskonto,
                BIC = entry.BIC,
                Purpose = purpose,
                BookingType = entry.Buchungstext,
                BookingDate = entry.Buchungstag,
                ValueDate = entry.Valutadatum,
                Currency = entry.Waehrung,
                PartnerIBAN = entry.IBAN
            };
        }

        private static Dictionary<string, string> ParseMT940Purposes(string sepaPurposes)
        {
            Dictionary<string, string> result = new();
            if (string.IsNullOrWhiteSpace(sepaPurposes))
                return result;

            // Collect all occuring SEPA purposes ordered by their position
            List<Tuple<int, string>> indices = new List<Tuple<int, string>>();
            foreach (string sepaPurpose in new string[] { "SVWZ", "ABWA" })
            {
                string prefix = $"{sepaPurpose}+";
                var idx = sepaPurposes.IndexOf(prefix);
                if (idx >= 0)
                {
                    indices.Add(Tuple.Create(idx, sepaPurpose));
                }
            }
            indices = indices.OrderBy(v => v.Item1).ToList();

            // Then get the values
            for (int i = 0; i < indices.Count; i++)
            {
                var beginIdx = indices[i].Item1 + $"{indices[i].Item2}+".Length;
                var endIdx = i < indices.Count - 1 ? indices[i + 1].Item1 : sepaPurposes.Length;

                var value = sepaPurposes.Substring(beginIdx, endIdx - beginIdx);
                result[indices[i].Item2] = value;
            }

            return result;
        }
    }
}
