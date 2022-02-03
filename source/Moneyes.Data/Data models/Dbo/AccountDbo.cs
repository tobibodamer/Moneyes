﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Moneyes.Data
{
    public class AccountDbo : UniqueEntity
    {
        /// <summary>
        /// The bank account number.
        /// </summary>
        public string Number { get; init; }

        /// <summary>
        /// Gets the bank entry this account belongs to.
        /// </summary>
        public BankDbo Bank { get; init; }

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
        /// Gets the FinTS account permissions.
        /// </summary>
        public IReadOnlyList<string> Permissions { get; init; }

        public static bool ContentEquals(AccountDbo left, AccountDbo other)
        {
            return other is not null &&
                   left.Number == other.Number &&
                   left.Bank?.Id == other.Bank?.Id &&
                   left.OwnerName == other.OwnerName &&
                   left.Type == other.Type &&
                   (left?.Permissions.SequenceEqual(other?.Permissions) ?? other.Permissions is null);
        }

        public override bool ContentEquals(UniqueEntity other)
        {
            return other is AccountDbo otherAccount 
                && ContentEquals(this, otherAccount);
        }

        public AccountDbo(
            Guid id, 
            string number, 
            DateTime? createdAt = null,
            DateTime? updatedAt = null,
            bool isDeleted = false) 
            : base(id, createdAt, updatedAt, isDeleted)
        {
            Number = number;
        }

        public AccountDbo(
            AccountDbo other,
            string number = null,
            Guid? id = null,
            DateTime? createdAt = null,
            DateTime? updatedAt = null,
            bool? isDeleted = null) 
            : base(other, id, createdAt, updatedAt, isDeleted)
        {
            Number = number ?? other.Number;
        }
    }
}
