using Moneyes.Core.Filters;

namespace Moneyes.Data
{
    public record ConditionFilterDto
    {
        public string Selector { get; init; }
        public ConditionOperator Operator { get; init; }
        public object[] Values { get; init; }
        public bool CaseSensitive { get; init; }
        public bool CompareAll { get; init; }
    }
}
