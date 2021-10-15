using System;

namespace Moneyes.Core
{
    /// <summary>
    /// Represents a banking transaction.
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// The date this transaction was valued.
        /// </summary>
        public DateTime? ValueDate { get; set; }

        /// <summary>
        /// The date this transaction was created.
        /// </summary>
        public DateTime BookingDate { get; set; }

        /// <summary>
        /// The purpose of this transaction.
        /// </summary>
        public string Purpose { get; set; }

        /// <summary>
        /// The booking type.
        /// </summary>
        public string BookingType { get; set; }

        /// <summary>
        /// The transaction amount.
        /// </summary>
        public decimal Amount { get; set; }

        #region Partner

        /// <summary>
        /// IBAN of the partner account.
        /// </summary>
        public string IBAN { get; set; }

        /// <summary>
        /// BIC of the partners bank.
        /// </summary>
        public string BIC { get; set; }

        /// <summary>
        /// The partner account name.
        /// </summary>
        public string PartnerName { get; set; }

        /// <summary>
        /// Alternative partner account name.
        /// </summary>
        public string AltPartnerName { get; set; }

        /// <summary>
        /// The city provided with the transaction.
        /// </summary>
        public string City => ParseCityAndCountry()?.City;

        /// <summary>
        /// The country code provided with the transaction.
        /// </summary>
        public string CountryCode => ParseCityAndCountry()?.Country;

        private (string City, string Country)? ParseCityAndCountry()
        {
            if (string.IsNullOrEmpty(AltPartnerName) || !AltPartnerName.Contains("//"))

            { return null; }

            var splitted = AltPartnerName.Split("//");

            if (splitted.Length != 2)
            {
                return null;
            }

            splitted = splitted[1].Split("/");


            if (splitted.Length == 0)
            {
                return null;
            }

            string city = splitted[0].Trim();

            if (splitted.Length == 1)
            {
                return (city, null);
            }

            string country = splitted[1].Trim();

            return (city, country);
        }

        public override bool Equals(object obj)
        {
            return obj is Transaction transaction &&
                   BookingDate == transaction.BookingDate &&
                   Purpose == transaction.Purpose &&
                   BookingType == transaction.BookingType &&
                   Amount == transaction.Amount &&
                   IBAN == transaction.IBAN &&
                   BIC == transaction.BIC &&
                   PartnerName == transaction.PartnerName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BookingDate, Purpose, BookingType, Amount, IBAN, BIC, PartnerName);
        }
        #endregion
    }
}
