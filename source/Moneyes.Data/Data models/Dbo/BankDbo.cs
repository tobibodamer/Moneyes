using System;
using System.Security;

namespace Moneyes.Data
{
    public record BankDbo : UniqueEntity<BankDbo>
    {
        public string Name { get; init; }

        /// <summary>
        /// The bank code of the bank to connect to.
        /// </summary>
        public int BankCode { get; init; }

        /// <summary>
        /// The bank identifier code (BIC).
        /// </summary>
        public string BIC { get; init; }

        /// <summary>
        /// Online banking username for the bank account.
        /// </summary>
        public string UserId { get; init; }

        /// <summary>
        /// Online banking logon pin.
        /// </summary>
        public SecureString Pin { get; init; }

        /// <summary>
        /// Online banking server url.
        /// </summary>
        public string Server { get; init; }

        /// <summary>
        /// The supported HBCI version of the bank.
        /// </summary>
        public int HbciVersion { get; init; }

        public override bool ContentEquals(BankDbo otherBank)
        {
            return
                Name == otherBank.Name &&
                BankCode == otherBank.BankCode &&
                BIC == otherBank.BIC &&
                UserId == otherBank.UserId &&
                (Pin?.Equals(otherBank.Pin) ?? otherBank.Pin is null) &&
                Server == otherBank.Server &&
                HbciVersion == otherBank.HbciVersion;
        }

        /// <summary>
        /// For deserialization only.
        /// </summary>
        protected BankDbo() { }

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
    }
}
