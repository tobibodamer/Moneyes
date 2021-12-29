using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.IO;
using System;

namespace Moneyes.LiveData
{
    /// <summary>
    /// Class to get bank institutes that support online banking with this application.
    /// </summary>
    public class BankInstitutes
    {
        internal const string FINTS_INSTITUTES_FILE = "fints_institute.csv";

        private static List<FinTsInstitute> _institutes;


#nullable enable
        internal static FinTsInstitute? GetInstituteInternal(int bankCode)
        {
            return GetInstitutes()
                .FirstOrDefault(institute => institute.BankCode.Equals(bankCode.ToString()));
        }
#nullable disable

        internal static IEnumerable<FinTsInstitute> GetInstitutes(string fileName = FINTS_INSTITUTES_FILE)
        {
            if (_institutes != null && _institutes.Any())
            {
                return _institutes;
            }

            var config = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                Encoding = Encoding.UTF8,
                DetectDelimiter = true,
                IgnoreBlankLines = true
            };

            using var reader = new StreamReader(fileName);
            using var csv = new CsvReader(reader, config);

            _institutes = csv.GetRecords<FinTsInstitute>().ToList();

            return _institutes;
        }

#nullable enable
        /// <summary>
        /// Gets the bank institute for the given bank code, or <see langword="null"/> if not found.
        /// </summary>
        /// <param name="bankCode">The bank code.</param>
        /// <returns></returns>
        public static IBankInstitute? GetInstitute(int bankCode)
        {
            return GetInstituteInternal(bankCode);
        }
#nullable disable

        /// <summary>
        /// Gets whether the bank given by the bank code is supported.
        /// </summary>
        /// <param name="bankCode"></param>
        /// <returns></returns>
        public static bool IsSupported(int bankCode)
        {
            var institute = GetInstituteInternal(bankCode);

            if (institute is null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(institute.FinTs_Url))
            {
                return false;
            }

            if (!Uri.TryCreate(institute.FinTs_Url, UriKind.Absolute, out _))
            {
                return false;
            }

            return true;
        }
    }
}
