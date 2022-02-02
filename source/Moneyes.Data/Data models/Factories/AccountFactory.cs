using Moneyes.Core;

namespace Moneyes.Data
{
    public class AccountFactory : IAccountFactory
    {
        private readonly IBankDetailsFactory _bankDetailsFactory;

        public AccountFactory(IBankDetailsFactory bankDetailsFactory)
        {
            _bankDetailsFactory = bankDetailsFactory;
        }

        public AccountDetails CreateFromDbo(AccountDbo accountDbo)
        {
            return new(
                id: accountDbo.Id, 
                number: accountDbo.Number, 
                bankDetails: _bankDetailsFactory.CreateFromDbo(accountDbo.Bank))
            {
                IBAN = accountDbo.IBAN,
                OwnerName = accountDbo.OwnerName,
                Type = accountDbo.Type,
            };
        }
    }
}
