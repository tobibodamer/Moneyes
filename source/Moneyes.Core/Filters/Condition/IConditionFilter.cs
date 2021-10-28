using System;
using System.Collections;
using System.Collections.Generic;

namespace Moneyes.Core
    .Filters
{
    /// <summary>
    /// Provides an evaluable condition filter for objects of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConditionFilter<T> : IEvaluable<T>
    {
        /// <summary>
        /// The name of the property that is targeted by the condition.
        /// </summary>
        string Selector { get; }

        /// <summary>
        /// The operator used for this condition.
        /// </summary>
        ConditionOperator Operator { get; }

        /// <summary>
        /// The values used for the condition.
        /// </summary>
        IEnumerable Values { get; }

        /// <summary>
        /// Gets whether the condition is case sensitive for string values.
        /// </summary>
        bool CaseSensitive { get; }

        /// <summary>
        /// Gets whether the condition filter should satisfy all values or any value.
        /// </summary>
        bool CompareAll { get; }
    }

}
