using Moneyes.Core;
using libfintx.FinTS.Swift;
using System;

namespace Test
{
    static class Conversion
    {
        public static Transaction FromLiveTransaction(SwiftTransaction swiftTransaction)
        {
            return new(id: Guid.NewGuid())
            {
                Purpose = swiftTransaction.SVWZ,
                Amount = swiftTransaction.Amount,
                IBAN = swiftTransaction.AccountCode,
                BIC = swiftTransaction.BankCode,
                Name = swiftTransaction.PartnerName,
                AltName = swiftTransaction.ABWA,
                BookingType = swiftTransaction.Text,
                BookingDate = swiftTransaction.InputDate,
                ValueDate = swiftTransaction.ValueDate
            };
        }
    }
}
