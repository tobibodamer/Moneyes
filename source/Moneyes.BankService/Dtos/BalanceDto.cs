using System;

namespace Moneyes.BankService.Dtos
{
    public record BalanceDto
    {        
        public DateTime Date { get; init; }
        
        public decimal Amount { get; init; }
    }
}
