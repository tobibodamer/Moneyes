using Moneyes.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Moneyes.BankService.Dtos
{
    public record CreateConnectionDto
    {
        [Required]
        [Range(1, 99999999)]
        /// <summary>
        /// The bank code of the bank to connect to.
        /// </summary>
        public int BankCode { get; init; }

        [Required(AllowEmptyStrings = false)]
        /// <summary>
        /// Online banking username for the bank account.
        /// </summary>
        public string UserId { get; init; }

        [Required(AllowEmptyStrings = false)]
        /// <summary>
        /// Online banking logon pin.
        /// </summary>
        public string Pin { get; init; }

        [Url]
        /// <summary>
        /// Online banking server uri.
        /// </summary>
        public Uri Server { get; init; }

        public bool TestConnection { get; init; } = true;
    }

    public record GetBalanceDto
    {
        [Range(0, int.MaxValue, ErrorMessage = "Please enter valid integer Number")]
        [Required(AllowEmptyStrings = false)]
        /// <summary>
        /// The bank account number.
        /// </summary>
        public string AccountNumber { get; init; }

        [Required(AllowEmptyStrings = false)]
        /// <summary>
        /// The international bank account number
        /// </summary>        
        public string IBAN { get; init; }
    }

    public record GetTransactionsDto : GetBalanceDto
    {
        [Required]
        public DateTime? StartDate { get; init; }

        public DateTime? EndDate { get; init; }
    }

    public record TransactionBalanceListDto
    {
        public IEnumerable<TransactionDto> Transactions { get; init; }

        public IEnumerable<BalanceDto> Balances { get; init; }
    }

    public record BalanceDto
    {        
        public DateTime Date { get; init; }
        
        public decimal Amount { get; init; }
    }

    public record TransactionDto
    {
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
