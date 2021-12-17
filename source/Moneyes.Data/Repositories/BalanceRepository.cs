using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Moneyes.Core;

namespace Moneyes.Data
{
    public class BalanceRepository : CachedRepository<Balance>
    {
        public BalanceRepository(IDatabaseProvider dbProvider) : base(dbProvider)
        {
        }

        /// <summary>
        /// Gets the balance that is closed to the given <paramref name="date"/>.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public Balance GetByDate(DateTime date, AccountDetails account)
        {
            return Cache.Values
                .Where(b => b.Account.IBAN.Equals(account.IBAN))
                .Where(b => b.Date <= date)
                .OrderByDescending(b => b.Date)
                .FirstOrDefault();
        }

        //public Balance GetByDate(DateTime date, string bankCode)
        //{
        //    var grouped = Collection.Query()
        //        .Where(b => b.Account.BankCode == bankCode)
        //        .Where(b => b.Date <= date)
        //        .OrderByDescending(b => b.Date)
        //        .ToEnumerable()
        //        .GroupBy(b => b.Account);

        //    var minDate = grouped.Min(g => g.Select(b => b.Date).FirstOrDefault());

        //    return grouped.Min()
        //}
    }
}
