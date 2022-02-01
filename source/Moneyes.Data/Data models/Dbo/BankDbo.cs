using System;
using System.Security;

namespace Moneyes.Data
{
    public class BankDbo : UniqueEntity
    {
        /// <summary>
        /// The bank code of the bank to connect to.
        /// </summary>
        public int BankCode { get; init; }

        /// <summary>
        /// The bank identifier code (BIC).
        /// </summary>
        public string BIC { get; set; }

        /// <summary>
        /// Online banking username for the bank account.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Online banking logon pin.
        /// </summary>
        public SecureString Pin { get; set; }

        /// <summary>
        /// Online banking server uri.
        /// </summary>
        public Uri Server { get; set; }

        /// <summary>
        /// The supported HBCI version of the bank.
        /// </summary>
        public int HbciVersion { get; set; }
    }
}
