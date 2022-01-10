using System;
using System.ComponentModel.DataAnnotations;

namespace Moneyes.BankService.Dtos
{
    public record GetBalanceDto
    {
        [Range(0, int.MaxValue)]
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
}
