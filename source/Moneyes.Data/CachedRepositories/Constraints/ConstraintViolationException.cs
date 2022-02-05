using System;

namespace Moneyes.Data
{
    /// <summary>
    /// Exception when a constraint is violated and <see cref="ConflictResolution.Fail"/> is chosen.
    /// </summary>
    public class ConstraintViolationException : Exception
    {
        public string PropertyName { get; }
        public object NewValue { get; }
        public object ExistingValue { get; }

        public ConstraintViolationException(
            string message, string propertyName, object newValue, object existingValue)
            : base(message)
        {
            PropertyName = propertyName;
            NewValue = newValue;
            ExistingValue = existingValue;
        }
    }
}
