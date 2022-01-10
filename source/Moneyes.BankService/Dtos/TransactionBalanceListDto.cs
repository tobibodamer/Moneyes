using System.Collections.Generic;

namespace Moneyes.BankService.Dtos
{
    public record TransactionBalanceListDto
    {
        public IEnumerable<TransactionDto> Transactions { get; init; }

        public IEnumerable<BalanceDto> Balances { get; init; }
    }
}
