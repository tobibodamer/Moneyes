using LiteDB;
using Moneyes.LiveData;
using System;

namespace Moneyes.Data
{
    public class BankConnectionStore
    {
        private readonly ILiteDatabase _db;
        public BankConnectionStore(ILiteDatabase db)
        {
            _db = db;
        }

        public bool HasBankingDetails => _db.GetCollection<OnlineBankingDetails>().Count() > 0;
        public bool SetBankingDetails(OnlineBankingDetails bankingDetails)
        {
            ILiteCollection<OnlineBankingDetails> collection = _db.GetCollection<OnlineBankingDetails>();

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
            ILiteCollection<OnlineBankingDetails> collection = _db.GetCollection<OnlineBankingDetails>();

            return collection.FindById(0);
        }
    }
}
