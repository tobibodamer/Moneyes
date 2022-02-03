namespace Moneyes.Data;

/// <summary>
/// Encapsulates information about the violation of a <see cref="IUniqueConstraint{T}"/>.
/// </summary>
public class ConstraintViolation<T>
{
    public ConstraintViolation(IUniqueConstraint<T> violatedConstraint, T existingEntity, T newEntity)
    {
        Constraint = violatedConstraint;
        ExistingEntity = existingEntity;
        NewEntity = newEntity;
    }

    /// <summary>
    /// The constraint that is violated.
    /// </summary>
    public IUniqueConstraint<T> Constraint { get; init; }

    /// <summary>
    /// The existing entity involved.
    /// </summary>
    public T ExistingEntity { get; init; }

    /// <summary>
    /// The new entity that caused the violation.
    /// </summary>
    public T NewEntity { get; init; }
}