using Moneyes.Core;

namespace Moneyes.Data
{
    public class BankDetailsRepository : CachedRepository<BankDetails>
    {
        public BankDetailsRepository(IDatabaseProvider databaseProvider) : base(databaseProvider)
        {
        }
    }
}
