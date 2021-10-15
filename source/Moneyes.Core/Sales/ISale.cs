using System;

namespace Moneyes.Core
{
    public interface ISale
    {
        DateTime PaymentDate { get; }
        DateTime BookingDate { get; }
        decimal Amount { get; }
        string City { get; }
        string Country { get; }
        string Name { get; }
        SaleType SaleType { get; }
        string BookingType { get; }

        string IntendedUse { get; }
        string IBAN { get; }
    }


}
