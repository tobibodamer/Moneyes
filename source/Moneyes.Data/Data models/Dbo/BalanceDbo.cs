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
                   left.Account.Id.Equals(other.Account.Id);
        }
    }
}
