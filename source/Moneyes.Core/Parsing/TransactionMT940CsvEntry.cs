using System;
using CsvHelper.Configuration.Attributes;

namespace Moneyes.Core.Parsing
{
    internal class TransactionMT940CsvEntry
    {
        [Name("Auftragskonto")]
        public string Auftragskonto { get; set; }

        [Optional]
        [Name("Buchungstag")]
        public DateTime Buchungstag { get; set; }

        [Name("Valutadatum")]
        public DateTime? Valutadatum { get; set; }

        [Name("Buchungstext")]
        public string Buchungstext { get; set; }

        [Name("Verwendungszweck")]
        public string Verwendungszweck { get; set; }

        [Name("Beguenstigter/Zahlungspflichtiger")]
        public string AccountName { get; set; }

        [Name("Kontonummer")]
        public string IBAN { get; set; }

        [Name("BLZ")]
        public string BIC { get; set; }

        [Name("Betrag")]
        public decimal Amount { get; set; }

        [Name("Waehrung")]
        public string Waehrung { get; set; }

        [Name("Info")]
        public string Info { get; set; }
    }


}
