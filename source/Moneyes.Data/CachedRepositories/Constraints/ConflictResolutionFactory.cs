using System;

namespace Moneyes.Data;

/// <summary>
/// A factory to create advanced <see cref="ConflictResolutionAction"/> instances.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class ConflictResolutionFactory<T>
{
    private readonly ConstraintViolation<T> _violation;

    internal ConflictResolutionFactory(ConstraintViolation<T> violation) =>
        _violation = violation;

    /// <summary>
    /// Use the default <see cref="ConflictResolution"/> for the repository or constraint.
    /// </summary>
    /// <returns></returns>
    public ConflictResolutionAction Default() => ConflictResolutionAction.Default();

    /// <summary>
    /// Creates a <see cref="ConflictResolution.Ignore"/> action.
    /// </summary>
    /// <returns></returns>
    public ConflictResolutionAction Ignore() => ConflictResolutionAction.Ignore();

    /// <summary>
    /// Creates a <see cref="ConflictResolution.Fail"/> action.
    /// </summary>
    /// <returns></returns>
    public ConflictResolutionAction Fail() => ConflictResolutionAction.Fail();

    /// <summary>
    /// Creates a <see cref="ConflictResolution.Replace"/> action.
    /// </summary>
    /// <returns></returns>
    public ConflictResolutionAction Replace() => ConflictResolutionAction.Replace();

    /// <summary>
    /// Creates an update conflic resolution action
    /// that updates an existing entity to resolve the conflict.
    /// </summary>        
    /// <returns></returns>
    public ConflictResolutionAction Update(Func<T, T, T> updateFactory) =>
        new UpdateConflicResolutionAction<T>(updateFactory);

    /// <summary>
    /// Creates a dynamic conflict resolution action based on the underlying violation.
    /// </summary>
    /// <param name="conflictResolutionDelegate"></param>
    /// <returns></returns>
    public ConflictResolutionAction Dynamic(Func<ConstraintViolation<T>, ConflictResolutionAction> getFromViolation) =>
        getFromViolation(_violation);
}
