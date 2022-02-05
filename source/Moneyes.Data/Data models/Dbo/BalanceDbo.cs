using System;

namespace Moneyes.Data
{
    public class BalanceDbo : UniqueEntity
    {
        public AccountDbo Account { get; init; }
        public DateTime Date { get; init; }
        public decimal Amount { get; init; }
        public string Currency { get; init; }

        public static bool ContentEquals(BalanceDbo left, BalanceDbo other)
        {
            return other is not null &&
                   left.Date == other.Date &&
                   left.Amount == other.Amount &&
                   left.Currency == other.Currency &&
                   left.Account?.Id == other.Account?.Id;
        }

        public override bool ContentEquals(UniqueEntity other)
        {
            return other is BalanceDbo otherBalance
                && ContentEquals(this, otherBalance);
        }

        /// <summary>
        /// For deserialization only.
        /// </summary>
        protected BalanceDbo() { }

        public BalanceDbo(
            Guid id,
            DateTime date,
            decimal amount,
            DateTime? createdAt = null,
            DateTime? updatedAt = null,
            bool isDeleted = false)
            : base(id, createdAt, updatedAt, isDeleted)
        {
            Date = date;
            Amount = amount;
        }

        public BalanceDbo(
            BalanceDbo other,
            DateTime? date = null,
            decimal? amount = null,
            Guid? id = null,
            DateTime? createdAt = null,
            DateTime? updatedAt = null,
            bool? isDeleted = null)
            : base(other, id, createdAt, updatedAt, isDeleted)
        {
            Date = date ?? other.Date;
            Amount = amount ?? other.Amount;
        }
    }
}
