using System;

namespace Moneyes.Core
{
    /// <summary>
    /// Represents account details for a bank account.
    /// </summary>
    public class AccountDetails
    {
        /// <summary>
        /// The bank account number.
        /// </summary>
        public string Number { get; init; }

        /// <summary>
        /// The bank identifier code.
        /// </summary>
        public string BIC { get; init; }
        
        /// <summary>
        /// The bank code.
        /// </summary>
        public string BankCode { get; init; }

        /// <summary>
        /// The international bank account number
        /// </summary>
        public string IBAN { get; init; }

        /// <summary>
        /// The name of the bank account owner.
        /// </summary>
        public string OwnerName { get; init; }

        /// <summary>
        /// The type / description / name of the bank account.
        /// </summary>
        public string Type { get; init; }

        public override bool Equals(object obj)
        {
            return obj is AccountDetails details &&
                   Number == details.Number &&
                   BIC == details.BIC &&
                   IBAN == details.IBAN &&
                   OwnerName == details.OwnerName &&
                   Type == details.Type;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Number, BIC, IBAN, OwnerName, Type);
        }
    }
}
