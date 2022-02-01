using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.Core
{
    public class Balance
    {
        public Guid Id { get; init; }
        public AccountDetails Account { get; init; }
        public DateTime Date { get; init; }
        public decimal Amount { get; init; }
        public string Currency { get; init; }
        public bool IsNegative => Amount < 0;

        public Balance(Guid id) => Id = id;
        public override bool Equals(object obj)
        {
            return obj is Balance balance &&
                   EqualityComparer<AccountDetails>.Default.Equals(Account, balance.Account) &&
                   Date == balance.Date &&
                   Amount == balance.Amount &&
                   Currency == balance.Currency &&
                   IsNegative == balance.IsNegative;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Account, Date, Amount, Currency, IsNegative);
        }
    }
}
