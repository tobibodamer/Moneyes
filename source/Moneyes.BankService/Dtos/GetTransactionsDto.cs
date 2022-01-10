using System;
using System.ComponentModel.DataAnnotations;

namespace Moneyes.BankService.Dtos
{
    public record GetTransactionsDto : GetBalanceDto
    {
        [Required]
        public DateTime? StartDate { get; init; }

        public DateTime? EndDate { get; init; }
    }
}
