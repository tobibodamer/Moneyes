using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Moneyes.Core
{
    /// <summary>
    /// Represents a banking transaction.
    /// </summary>
    public class Transaction
    {
        private readonly int _index;
        private Lazy<string> _idLazy;
        private string _id;
        public string UID
        {
            get
            {
                return _idLazy.Value;
            }
            init
            {
                _id = value;
            }
        }

        /// <summary>
        /// The date this transaction was valued.
        /// </summary>
        public DateTime? ValueDate { get; init; }

        /// <summary>
        /// The date this transaction was created.
        /// </summary>
        public DateTime BookingDate { get; init; }

        /// <summary>
        /// The original date when this transaction was caused.
        /// </summary>
        public DateTime? OriginalDate => ParseDate();

        /// <summary>
        /// The purpose of this transaction.
        /// </summary>
        [FilterProperty("Purpose")]
        public string Purpose { get; init; }

        /// <summary>
        /// The booking type.
        /// </summary>
        [FilterProperty("Booking type")]
        public string BookingType { get; init; }

        /// <summary>
        /// The transaction amount.
        /// </summary>
        [FilterProperty("Amount")]
        public decimal Amount { get; init; }

        /// <summary>
        /// The type of this transaction.
        /// </summary>
        public TransactionType Type => Amount > 0
            ? TransactionType.Income
            : TransactionType.Expense;

        public string Currency { get; init; }

        /// <summary>
        /// IBAN of the account this transaction belongs to.
        /// </summary>        
        public string IBAN { get; init; }

        #region Partner

        /// <summary>
        /// IBAN of the partners account.
        /// </summary>
        [FilterProperty("IBAN")]
        public string PartnerIBAN { get; init; }

        /// <summary>
        /// BIC of the partners bank.
        /// </summary>
        [FilterProperty("BIC")]
        public string BIC { get; init; }

        /// <summary>
        /// The partner account name.
        /// </summary>
        [FilterProperty("Name")]
        public string Name { get; init; }

        /// <summary>
        /// Alternative partner account name.
        /// </summary>
        [FilterProperty("Alt. Name")]
        public string AltName { get; init; }

        #endregion

        /// <summary>
        /// The city provided with the transaction.
        /// </summary>
        [FilterProperty("City")]
        public string City => ParseCityAndCountry()?.City;

        /// <summary>
        /// The country code provided with the transaction.
        /// </summary>
        public string CountryCode => ParseCityAndCountry()?.Country;
        /// <summary>
        /// Gets the categories of this transaction.
        /// </summary>
        public List<Category> Categories { get; set; } = new();

        /// <summary>
        /// Gets the index of this transactions. For identical transactions only!
        /// </summary>
        public int Index
        {
            get => _index;
            init
            {
                _index = value;
                _idLazy = new(GenerateUID);
            }
        }

        public Transaction()
        {
            _idLazy = new(() => _id ?? GenerateUID());
        }

        private DateTime? ParseDate()
        {
            if (Purpose == null)
            {
                return null;
            }

            Regex rx = new(@"^(\d{4}-\d{2}-\d{2}T\d{2}\.\d{2})");
            Match match = rx.Match(Purpose);

            if (!match.Success)
            {
                return null;
            }

            if (DateTime.TryParseExact(match.Value, "yyyy-MM-ddTHH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date)
                || DateTime.TryParseExact(match.Value, "yyyy-MM-ddTHH.mm",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date))
            {
                return date;
            };

            return null;
        }

        private (string City, string Country)? ParseCityAndCountry()
        {
            if (string.IsNullOrEmpty(AltName) || !AltName.Contains("//"))

            { return null; }

            var splitted = AltName.Split("//");

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

        private string GenerateUID()
        {
            //return HashCode.Combine(BookingDate, Purpose, BookingType, Amount, IBAN, BIC, Name, Index);
            string s = $"{BookingDate:yyyyMMdd}{Purpose}{BookingType}{Amount}{IBAN}{BIC}{Name}{Index}"
                .Replace(" ", "")
                .Replace(":", ".");

            using SHA256 mySHA256 = SHA256.Create();
            return BitConverter.ToString(mySHA256.ComputeHash(System.Text.Encoding.ASCII.GetBytes(s))).Replace("-", "");
        }

        public void RegenerateUID()
        {
            _idLazy = new Lazy<string>(() => GenerateUID());
        }

        /// <summary>
        /// Gets the unique identifier of this transaction.
        /// </summary>
        /// <returns></returns>
        public string GetUID()
        {
            return _idLazy.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is Transaction transaction &&
                   BookingDate == transaction.BookingDate &&
                   ValueDate == transaction.ValueDate &&
                   Purpose == transaction.Purpose &&
                   BookingType == transaction.BookingType &&
                   Amount == transaction.Amount &&
                   IBAN == transaction.IBAN &&
                   PartnerIBAN == transaction.PartnerIBAN &&
                   BIC == transaction.BIC &&
                   Name == transaction.Name &&
                   Categories.SequenceEqual(transaction.Categories) &&
                   _index.Equals(transaction._index) &&
                   Currency == transaction.Currency;
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(BookingDate);
            hash.Add(ValueDate);
            hash.Add(Purpose);
            hash.Add(BookingType);
            hash.Add(Amount);
            hash.Add(IBAN);
            hash.Add(PartnerIBAN);
            hash.Add(BIC);
            hash.Add(Name);
            hash.Add(Categories);
            hash.Add(_index);
            hash.Add(Currency);

            return hash.ToHashCode();
        }
    }
}
