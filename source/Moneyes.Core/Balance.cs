using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.Core
{
    public class Balance
    {
        public AccountDetails Account { get; init; }
        public DateTime Date { get; init; }
        public decimal Amount { get; init; }
        public string Currency { get; init; }
        public bool IsNegative => Amount < 0;
    }
}
