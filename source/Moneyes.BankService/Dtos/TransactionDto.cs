using System;

namespace Moneyes.BankService.Dtos
{
    public record TransactionDto
    {
        public string UID { get; init; }
        public DateTime? ValueDate { get; init; }
        public DateTime BookingDate { get; init; }

        public string Purpose { get; init; }

        public string BookingType { get; init; }

        public decimal Amount { get; init; }

        public string Currency { get; init; }

        public string IBAN { get; init; }

        #region Partner

        public string PartnerIBAN { get; init; }

        public string BIC { get; init; }
        public string Name { get; init; }

        public string AltName { get; init; }

        #endregion

        public int Index { get; init; }

    }
}
