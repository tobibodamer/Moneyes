using System;

namespace MoneyesParser
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
    }


}
