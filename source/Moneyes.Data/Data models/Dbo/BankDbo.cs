using System;
using System.Security;

namespace Moneyes.Data
{
    public class BankDbo : UniqueEntity
    {
        public string Name { get; set; }

        /// <summary>
        /// The bank code of the bank to connect to.
        /// </summary>
        public int BankCode { get; init; }

        /// <summary>
        /// The bank identifier code (BIC).
        /// </summary>
        public string BIC { get; set; }

        /// <summary>
        /// Online banking username for the bank account.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Online banking logon pin.
        /// </summary>
        public SecureString Pin { get; set; }

        /// <summary>
        /// Online banking server url.
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// The supported HBCI version of the bank.
        /// </summary>
        public int HbciVersion { get; set; }

        public override bool ContentEquals(UniqueEntity other)
        {
            return other is BankDbo otherBank &&
                Name == otherBank.Name &&
                BankCode == otherBank.BankCode &&
                BIC == otherBank.BIC &&
                UserId == otherBank.UserId &&
                (Pin?.Equals(otherBank.Pin) ?? otherBank.Pin is null) &&
                Server == otherBank.Server &&
                HbciVersion == otherBank.HbciVersion;
        }

        public BankDbo(
            Guid id,
            int bankCode,
            DateTime? createdAt = null,
            DateTime? updatedAt = null,
            bool isDeleted = false)
            : base(id, createdAt, updatedAt, isDeleted)
        {
            BankCode = bankCode;
        }

        public BankDbo(
            BankDbo other,
            int? bankCode = null,
            Guid? id = null,
            DateTime? createdAt = null,
            DateTime? updatedAt = null,
            bool? isDeleted = null)
            : base(other, id, createdAt, updatedAt, isDeleted)
        {
            BankCode = bankCode ?? other.BankCode;            
        }
    }
}
