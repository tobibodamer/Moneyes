using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Moneyes.Core;

namespace Moneyes.Data
{
    public class BalanceRepository : BaseRepository<Balance>
    {
        public BalanceRepository(ILiteDatabase db) : base(db)
        {            
        }

        /// <summary>
        /// Gets the balance that is closed to the given <paramref name="date"/>.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public Balance GetByDate(DateTime date, AccountDetails account)
        {
            return Collection.Query()
                .Where(b => b.Account.IBAN.Equals(account.IBAN))
                .Where(b => b.Date <= date)                
                .OrderByDescending(b => b.Date)
                .FirstOrDefault();
        }
    }
}
