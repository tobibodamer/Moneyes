using System;

namespace Moneyes.LiveData
{
    public class AccountDetails
    {
        public string Number { get; init; }
        public string BIC { get; init; }
        public string IBAN { get; init; }
        public string OwnerName { get; init; }
        public string Type { get; init; }

        public override bool Equals(object obj)
        {
            return obj is AccountDetails details &&
                   Number == details.Number &&
                   BIC == details.BIC &&
                   IBAN == details.IBAN &&
                   OwnerName == details.OwnerName &&
                   Type == details.Type;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Number, BIC, IBAN, OwnerName, Type);
        }
    }
}
