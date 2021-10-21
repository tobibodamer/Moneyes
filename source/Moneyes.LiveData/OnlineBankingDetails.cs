using System.Security;

namespace Moneyes.LiveData
{
    /// <summary>
    /// Details to access online banking.
    /// </summary>
    public class OnlineBankingDetails
    {
        /// <summary>
        /// The bank code of the bank to connect to.
        /// </summary>
        public int BankCode { get; init; }

        /// <summary>
        /// Online banking username for the bank account.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Online banking logon pin.
        /// </summary>
        public SecureString Pin { get; set; }
    }
}
