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

            var transactions = csv.GetRecords<SaleEntry>().ToList()
                .Select(record => FromCsvTransaction(record));

            return transactions;
        }

        private static Transaction FromCsvTransaction(SaleEntry entry)
        {
            string shortPartnerName = entry.AccountName;

            if (entry.AccountName.Contains("//"))
            {
                 shortPartnerName = entry.AccountName.Split("//").First().Trim();
            }

            return new()
            {
                Amount = entry.Amount,
                AltPartnerName = entry.AccountName,
                PartnerName = shortPartnerName,
                IBAN = entry.IBAN,
                BIC = entry.BIC,
                Purpose = entry.Verwendungszweck,
                BookingType = entry.Buchungstext,
                BookingDate = entry.Buchungstag,
                ValueDate = entry.Valutadatum
            };
        }
    }


}
