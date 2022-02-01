using Moneyes.Core;
using System.Linq;

namespace Moneyes.Data
{
    public class TransactionFactory : ITransactionFactory
    {
        private readonly ICategoryFactory _categoryFactory;

        public TransactionFactory(ICategoryFactory categoryFactory)
        {
            _categoryFactory = categoryFactory;
        }

        public Transaction CreateFromDbo(TransactionDbo transactionDbo)
        {
            return new Transaction(id: transactionDbo.Id)
            {
                AltName = transactionDbo.AltName,
                Amount = transactionDbo.Amount,
                IBAN = transactionDbo.IBAN,
                BIC = transactionDbo.BIC,
                BookingDate = transactionDbo.BookingDate,
                BookingType = transactionDbo.BookingType,
                Categories = transactionDbo.Categories?.Select(c => _categoryFactory.CreateFromDbo(c)).ToList() ?? null,
                Currency = transactionDbo.Currency,
                Index = transactionDbo.Index,
                Name = transactionDbo.Name,
                PartnerIBAN = transactionDbo.PartnerIBAN,
                UID = transactionDbo.UID,
                Purpose = transactionDbo.Purpose,
                ValueDate = transactionDbo.ValueDate
            };
        }
    }
}
