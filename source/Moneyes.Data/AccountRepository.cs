using LiteDB;
using Moneyes.Core;
using System.Collections.Generic;

namespace Moneyes.Data
{
    public class AccountRepository : CachedRepository<AccountDetails>
    {
        public AccountRepository(ILiteDatabase db) : base(db)
        {
        }

        public IEnumerable<AccountDetails> GetByBankCode(string bankCode)
        {
            return Collection.Find(acc => acc.BankCode.Equals(bankCode));
        }
            
    }
}
