using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.IO;

namespace Moneyes.LiveData
{
    public class FinTsInstitutes
    {
        public const string FINTS_INSTITUTES_FILE = "fints_institute.csv";

        private static List<FinTsInstitute> _institutes;


#pragma warning disable CS8632 // Die Anmerkung für Nullable-Verweistypen darf nur in Code innerhalb eines #nullable-Anmerkungskontexts verwendet werden.
        internal static FinTsInstitute? GetInstituteInternal(int bankCode)
#pragma warning restore CS8632 // Die Anmerkung für Nullable-Verweistypen darf nur in Code innerhalb eines #nullable-Anmerkungskontexts verwendet werden.
        {
            return GetInstitutes()
                .FirstOrDefault(institute => institute.BankCode.Equals(bankCode.ToString()));
        }

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
        public static IBankInstitute? GetInstitute(int bankCode)
        {
            return GetInstituteInternal(bankCode);
        }
#nullable disable
    }
}
