using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper.Configuration.Attributes;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Moneyes.Core.Parsing
{
    /// <summary>
    /// Represents a POCO for a sale.
    /// </summary>
    class SaleEntry
    {
        [Name("Auftragskonto")]
        public string Auftragskonto { get; set; }

        [Name("Buchungstag")]
        public DateTime Buchungstag { get; set; }

        [Name("Valutadatum")]
        public DateTime? Valutadatum { get; set; }

        [Name("Buchungstext")]
        public string Buchungstext { get; set; }

        [Name("Verwendungszweck")]
        public string Verwendungszweck { get; set; }

        [Name("Glaeubiger ID")]
        public string CreditorID { get; set; }

        [Name("Mandatsreferenz")]
        public string Mandatsreferenz { get; set; }

        [Name("Kundenreferenz (End-to-End)")]
        public string CustomerReference { get; set; }

        [Name("Sammlerreferenz")]
        public string Sammlerreferenz { get; set; }

        [Name("Lastschrift Ursprungsbetrag")]
        public string LastschriftUrsprungsbetrag { get; set; }

        [Name("Auslagenersatz Ruecklastschrift")]
        public string Auslagenersatz { get; set; }

        [Name("Beguenstigter/Zahlungspflichtiger")]
        public string AccountName { get; set; }

        [Name("Kontonummer/IBAN")]
        public string IBAN { get; set; }

        [Name("BIC (SWIFT-Code)")]
        public string BIC { get; set; }

        [Name("Betrag")]
        public decimal Amount { get; set; }

        [Name("Waehrung")]
        public string Waehrung { get; set; }

        [Name("Info")]
        public string Info { get; set; }
    }


}
