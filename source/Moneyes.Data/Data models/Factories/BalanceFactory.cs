using Moneyes.Core;

namespace Moneyes.Data
{
    public class BalanceFactory : IBalanceFactory
    {
        private readonly IAccountFactory _accountFactory;

        public BalanceFactory(IAccountFactory accountFactory)
        {
            _accountFactory = accountFactory;
        }

        public Balance CreateFromDbo(BalanceDbo balanceDbo)
        {
            return new(balanceDbo.Id)
            {
                Date = balanceDbo.Date,
                Amount = balanceDbo.Amount,
                Currency = balanceDbo.Currency,
                Account = _accountFactory.CreateFromDbo(balanceDbo.Account),
            };
        }
    }
}
