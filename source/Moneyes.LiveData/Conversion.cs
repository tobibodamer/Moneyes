using Moneyes.Core;
using libfintx.FinTS.Swift;
using libfintx.FinTS;

namespace Moneyes.LiveData
{
    static class Conversion
    {
        public static Core.Transaction FromLiveTransaction(
            SwiftTransaction swiftTransaction, 
            AccountInformation account)
        {
            return new()
            {
                Purpose = swiftTransaction.SVWZ,
                Amount = swiftTransaction.Amount,
                IBAN = account?.AccountIban,
                BIC = swiftTransaction.BankCode,
                PartnerName = swiftTransaction.PartnerName,
                AltPartnerName = swiftTransaction.ABWA,
                BookingType = swiftTransaction.Text,
                BookingDate = swiftTransaction.InputDate,
                ValueDate = swiftTransaction.ValueDate
            };
        }
    }
}
