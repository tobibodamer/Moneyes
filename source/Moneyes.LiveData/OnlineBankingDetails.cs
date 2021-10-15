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
        public int BankCode { get; set; }

        /// <summary>
        /// The account number of the desired bank account.
        /// </summary>
        public string AccountNumber { get; set; }

        /// <summary>
        /// Online banking username for the bank account.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Online banking logon pin.
        /// </summary>
        public string Pin { get; set; }
    }
}
