using LiteDB;
using Moneyes.LiveData;
using System;

namespace Moneyes.Data
{
    public class BankConnectionStore : IBankConnectionStore
    {
        private Lazy<ILiteDatabase> _dbLazy;
        private ILiteDatabase DB => _dbLazy.Value;
        public BankConnectionStore(IDatabaseProvider databaseProvider)
        {
            _dbLazy = new(() => databaseProvider.Database);
        }

        public bool HasBankingDetails => DB.GetCollection<OnlineBankingDetails>().Count() > 0;
        public bool SetBankingDetails(OnlineBankingDetails bankingDetails)
        {
            ILiteCollection<OnlineBankingDetails> collection = DB.GetCollection<OnlineBankingDetails>();

            if (bankingDetails is null)
            {
                return collection.Delete(0);
            }

            ValidateBankingDetails(bankingDetails);

            return collection.Upsert(0, bankingDetails);
        }

        private static void ValidateBankingDetails(OnlineBankingDetails bankingDetails)
        {
            if (string.IsNullOrEmpty(bankingDetails.UserId))
            {
                throw new ArgumentException("Online banking details must contain valid user id.");
            }
            else if (bankingDetails.BankCode.ToString().Length != 8)
            {
                throw new ArgumentException("Online banking details must contain valid bank code.");
            }
        }

        public OnlineBankingDetails GetBankingDetails()
        {
            ILiteCollection<OnlineBankingDetails> collection = DB.GetCollection<OnlineBankingDetails>();

            return collection.FindById(0);
        }
    }
}
