using System;
using System.Collections.Generic;

namespace Moneyes.Data
{
    public record TransactionDbo : UniqueEntity<TransactionDbo>
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
        /// Gets the category of this transaction.
        /// </summary>
#nullable enable
        public CategoryDbo? Category { get; set; }
#nullable disable

        /// <summary>
        /// Gets the index of this transactions. For identical transactions only!
        /// </summary>
        public int Index { get; init; }

        public static bool ContentEquals(TransactionDbo left, TransactionDbo right)
        {
            return
                   left.ValueDate == right.ValueDate &&
                   left.Purpose == right.Purpose &&
                   left.BookingType == right.BookingType &&
                   left.IBAN == right.IBAN &&
                   left.PartnerIBAN == right.PartnerIBAN &&
                   left.BIC == right.BIC &&
                   left.Name == right.Name &&
                   left.Currency == right.Currency &&
                   left.UID == right.UID &&
                   left.Category?.Id == right.Category?.Id;
        }

        public override bool ContentEquals(TransactionDbo other)
        {
            return ContentEquals(this, other);
        }

        /// <summary>
        /// For deserialization only.
        /// </summary>
        protected TransactionDbo() { }

        public TransactionDbo(
            Guid id,
            string uid,            
            DateTime? createdAt = null,
            DateTime? updatedAt = null,
            bool isDeleted = false)
            : base(id, createdAt, updatedAt, isDeleted)
        {
            UID = uid;
        }
    }
}
