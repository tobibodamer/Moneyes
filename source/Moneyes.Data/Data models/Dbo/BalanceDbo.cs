using System;

namespace Moneyes.Data
{
    public record BalanceDbo : UniqueEntity<BalanceDbo>
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

        public override bool ContentEquals(BalanceDbo other)
        {
            return ContentEquals(this, other);
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
    }
}
