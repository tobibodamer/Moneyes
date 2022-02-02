using Moneyes.Core;

namespace Moneyes.Data
{
    public class BankDetailsFactory : IBankDetailsFactory
    {
        public BankDetails CreateFromDbo(BankDbo bankDbo)
        {
            return new(bankDbo.Id, bankDbo.BankCode)
            {
                BIC = bankDbo.BIC,
                HbciVersion = bankDbo.HbciVersion,
                Server = bankDbo.Server,
                UserId = bankDbo.UserId,
                Pin = bankDbo.Pin?.Copy(),
            };
        }
    }
}
