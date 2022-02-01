using Moneyes.Core;

namespace Moneyes.Data
{
    public interface IAccountFactory
    {
        AccountDetails CreateFromDbo(AccountDbo accountDbo);
    }
}
