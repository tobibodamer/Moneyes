using Moneyes.Core;
using libfintx.FinTS.Swift;
using libfintx.FinTS;

namespace Moneyes.LiveData
{
    internal static class Conversion
    {
        /// <summary>
        /// Converts a <see cref="SwiftTransaction"/> to a <see cref="Moneyes.Core.Transaction"/>.
        /// </summary>
        /// <param name="swiftTransaction"></param>
        /// <param name="account"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Core.Transaction FromLiveTransaction(
            SwiftTransaction swiftTransaction,
            AccountDetails account,
            string currency,
            int index = 0)
        {
            swiftTransaction.SepaPurposes.TryGetValue(SepaPurpose.SVWZ, out var purpose);

            return new()
            {
                Index = index,
                Purpose = purpose,
                Amount = swiftTransaction.Amount,
                PartnerIBAN = swiftTransaction.AccountCode,
                BIC = swiftTransaction.BankCode,
                Name = swiftTransaction.PartnerName,
                AltName = swiftTransaction.ABWA,
                BookingType = swiftTransaction.Text,
                BookingDate = swiftTransaction.InputDate,
                ValueDate = swiftTransaction.ValueDate,
                IBAN = account.IBAN,
                Currency = currency
            };
        }
    }
}
