using Moneyes.Core.Filters;
using System.Collections.Generic;

namespace Moneyes.Data
{
    public record FilterGroupDto
    {
        public LogicalOperator Operator { get; init; } = LogicalOperator.And;
        public IReadOnlyList<ConditionFilterDto> Conditions { get; init; }
        public IReadOnlyList<FilterGroupDto> ChildFilters { get; init; }
    }
}
