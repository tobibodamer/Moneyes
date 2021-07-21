namespace MoneyesParser.Filters
{
    /// <summary>
    /// Provides a method to evaluate a predicate with input of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEvaluable<in T>
    {
        /// <summary>
        /// Evaluates a predicate against an input.
        /// </summary>
        /// <param name="input">The input to evaluate.</param>
        /// <returns>Whether the input satisfies the predicate.</returns>
        bool Evaluate(T input);
    }
}
