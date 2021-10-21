using Moneyes.Core;
using libfintx.FinTS.Swift;
using libfintx.FinTS;

namespace Moneyes.LiveData
{
    static class Conversion
    {
        public static Core.Transaction FromLiveTransaction(
            SwiftTransaction swiftTransaction,
            AccountDetails account,
            int index = 0)
        {
            return new()
            {
                Index = index,
                Purpose = swiftTransaction.SVWZ,
                Amount = swiftTransaction.Amount,
                IBAN = account?.IBAN,
                BIC = swiftTransaction.BankCode,
                Name = swiftTransaction.PartnerName,
                AltName = swiftTransaction.ABWA,
                BookingType = swiftTransaction.Text,
                BookingDate = swiftTransaction.InputDate,
                ValueDate = swiftTransaction.ValueDate,
                AccountNumber = account.Number
            };
        }
    }
}
