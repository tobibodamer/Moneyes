using Moneyes.Core;
using System;

namespace Moneyes.Data
{
    public record TransactionFilterDto
    {
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }
        public TransactionType TransactionType { get; init; }
#nullable enable
        public string? AccountNumber { get; init; }
#nullable disable

        public decimal? MinAmount { get; init; }
        public decimal? MaxAmount { get; init; }

        public FilterGroupDto Criteria { get; init; } = new();
    }
}
