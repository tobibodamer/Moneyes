using Moneyes.Core;

namespace Moneyes.Data
{
    public interface ITransactionFactory
    {
        Transaction CreateFromDbo(TransactionDbo transactionDbo);
    }
}
