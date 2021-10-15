using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.IO;

namespace Moneyes.LiveData
{
    class FinTsInstitutes
    {
        public const string FINTS_INSTITUTES_FILE = "fints_institute.csv";

        private static List<FinTsInstitute> _institutes;


#pragma warning disable CS8632 // Die Anmerkung für Nullable-Verweistypen darf nur in Code innerhalb eines #nullable-Anmerkungskontexts verwendet werden.
        public static FinTsInstitute? GetInstitute(int bankCode)
#pragma warning restore CS8632 // Die Anmerkung für Nullable-Verweistypen darf nur in Code innerhalb eines #nullable-Anmerkungskontexts verwendet werden.
        {
            return GetInstitutes()
                .FirstOrDefault(institute => institute.BankCode.Equals(bankCode));
        }

        public static IEnumerable<FinTsInstitute> GetInstitutes(string fileName = FINTS_INSTITUTES_FILE)
        {
            if (_institutes != null && _institutes.Any())
            {
                return _institutes;
            }

            var config = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                Encoding = Encoding.UTF8,
                DetectDelimiter = true
            };

            using var reader = new StreamReader(fileName);
            using var csv = new CsvReader(reader, config);

            _institutes = csv.GetRecords<FinTsInstitute>().ToList();

            return _institutes;
        }
    }
}
