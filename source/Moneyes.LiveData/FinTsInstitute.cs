using CsvHelper.Configuration.Attributes;

namespace Moneyes.LiveData
{
    class FinTsInstitute
    {
        [Name("BLZ")]
        public int BankCode { get; set; }

        [Name("Institut")]
        public string Institute { get; set; }

        [Name("Ort")]
        public string City { get; set; }

        [Name("Organisation")]
        public string Organisation { get; set; }

        [Name("HBCI-Zugang DNS")]
        public string HBCI_DNS { get; set; }

        [Name("HBCI- Zugang     IP-Adresse")]
        public string HBCI_IP { get; set; }

        [Name("HBCI-Version")]
        public string HBCI_Version { get; set; }

        [Name("PIN/TAN-Zugang URL")]
        public string FinTs_Url { get; set; }

        [Name("Version")]
        public string Version { get; set; }
    }
}
