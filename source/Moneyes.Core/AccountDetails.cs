using System;
using System.Collections.Generic;

namespace Moneyes.Core
{
    /// <summary>
    /// Represents account details for a bank account.
    /// </summary>
    public class AccountDetails
    {
        /// <summary>
        /// Uniquely identifies this accounts.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// The bank account number.
        /// </summary>
        public string Number { get; init; }

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

        /// <summary>
        /// Gets the bank entry this account belongs to.
        /// </summary>
        public BankDetails BankDetails { get; init; }

        public IReadOnlyList<string> Permissions { get; init; } 

        public AccountDetails(Guid id, string number, BankDetails bankDetails)
        {
            Id = id;
            Number = number;
            BankDetails = bankDetails;
        }

        public override bool Equals(object obj)
        {
            return obj is AccountDetails details &&
                   Number == details.Number &&
                   IBAN == details.IBAN &&
                   OwnerName == details.OwnerName &&
                   Type == details.Type;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Number, IBAN, OwnerName, Type);
        }
    }
}
