namespace Moneyes.LiveData
{
    /// <summary>
    /// Represents a bank institute.
    /// </summary>
    public interface IBankInstitute
    {
        /// <summary>
        /// The bank code.
        /// </summary>
        string BankCode { get; set; }

        /// <summary>
        /// The city of this bank.
        /// </summary>
        string City { get; set; }

        /// <summary>
        /// The name of the bank institute.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The organisation responsible for this bank.
        /// </summary>
        string Organisation { get; set; }
    }
}