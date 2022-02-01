using Moneyes.Core;

namespace Moneyes.Data
{
    public interface IBankDetailsFactory
    {
        BankDetails CreateFromDbo(BankDbo bankDbo);
    }
}