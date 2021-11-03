using System.ComponentModel;

namespace Moneyes.Core.Filters
{
    /// <summary>
    /// Conditional operators.
    /// </summary>
    public enum ConditionOperator
    {
        [Description("equals")]
        Equal,
        [Description(">")]
        Greater,
        [Description("<")]
        Smaller,
        [Description(">=")]
        GreaterOrEqual,
        [Description("<=")]
        SmallerOrEqual,
        [Description("doesn't equal")]
        NotEqual,
        [Description("begins with")]
        BeginsWith,
        [Description("contains")]
        Contains,
        [Description("doesn't contain")]
        DoesNotContain,
        [Description("ends with")]
        EndsWith
    }

}
