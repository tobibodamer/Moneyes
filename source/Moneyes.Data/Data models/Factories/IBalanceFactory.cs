using Moneyes.Core;

namespace Moneyes.Data
{
    public interface IBalanceFactory
    {
        Balance CreateFromDbo(BalanceDbo balanceDbo);
    }
}
