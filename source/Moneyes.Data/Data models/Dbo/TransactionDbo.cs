﻿using System;
using System.Collections.Generic;

namespace Moneyes.Data
{
    public class TransactionDbo : UniqueEntity
    {
        public string UID { get; init; }

        /// <summary>
        /// The date this transaction was valued.
        /// </summary>
        public DateTime? ValueDate { get; init; }

        /// <summary>
        /// The date this transaction was created.
        /// </summary>
        public DateTime BookingDate { get; init; }

        /// <summary>
        /// The purpose of this transaction.
        /// </summary>
        public string Purpose { get; init; }

        /// <summary>
        /// The booking type.
        /// </summary>
        public string BookingType { get; init; }

        /// <summary>
        /// The transaction amount.
        /// </summary>
        public decimal Amount { get; init; }
        public string Currency { get; init; }

        /// <summary>
        /// IBAN of the account this transaction belongs to.
        /// </summary>        
        public string IBAN { get; init; }

        #region Partner

        /// <summary>
        /// IBAN of the partners account.
        /// </summary>
        public string PartnerIBAN { get; init; }

        /// <summary>
        /// BIC of the partners bank.
        /// </summary>
        public string BIC { get; init; }

        /// <summary>
        /// The partner account name.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Alternative partner account name.
        /// </summary>
        public string AltName { get; init; }

        #endregion

        /// <summary>
        /// Gets the categories of this transaction.
        /// </summary>
        public List<CategoryDbo> Categories { get; set; } = new();

        /// <summary>
        /// Gets the index of this transactions. For identical transactions only!
        /// </summary>
        public int Index { get; init; }

        public static bool ContentEquals(TransactionDbo left, TransactionDbo other)
        {
            return other is TransactionDbo transaction &&
                   left.ValueDate == transaction.ValueDate &&
                   left.Purpose == transaction.Purpose &&
                   left.BookingType == transaction.BookingType &&
                   left.IBAN == transaction.IBAN &&
                   left.PartnerIBAN == transaction.PartnerIBAN &&
                   left.BIC == transaction.BIC &&
                   left.Name == transaction.Name &&
                   left.Currency == transaction.Currency &&
                   left.UID == transaction.UID;
        }
    }
}