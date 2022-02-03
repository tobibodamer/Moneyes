namespace Moneyes.Data;

/// <summary>
/// Provides conflict resolution actions for <see cref="UniqueEntity"/> types.
/// </summary>
public class UniqueConflictResolutionAction
{
    /// <summary>
    /// Updating an existing entity while keeping the existing id and creation timestamp.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="v"></param>
    /// <returns></returns>
    public static ConflictResolutionAction Update<T>(ConstraintViolation<T> v) where T : UniqueEntity
    {
        // Apply existing id and creation date to update existing entity
        v.NewEntity.Id = v.ExistingEntity.Id;
        v.NewEntity.CreatedAt = v.ExistingEntity.CreatedAt;

        return ConflictResolutionAction.Update(v.NewEntity);
    }

    /// <summary>
    /// Updating an existing entity when the content changed, while keeping the existing id and creation timestamp. <br></br>
    /// Uses the <see cref="UniqueEntity.ContentEquals(UniqueEntity)"/> method to compare the content.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="v">The constraint violation that needs to be resolved.</param>
    /// <param name="defaultResolution">The default <see cref="ConflictResolution"/> if the content equals.</param>
    /// <returns></returns>
    public static ConflictResolutionAction UpdateContent<T>(ConstraintViolation<T> v,
        ConflictResolution? defaultResolution = null) where T : UniqueEntity
    {
        if (v.ExistingEntity.ContentEquals(v.NewEntity))
        {
            return defaultResolution != null
                ? new(defaultResolution.Value)
                : ConflictResolutionAction.Default();
        }

        return Update(v);
    }

    /// <summary>
    /// Updating an existing entity when the content changed, while keeping the existing id and creation timestamp. <br></br>
    /// Uses the <see cref="UniqueEntity.ContentEquals(UniqueEntity)"/> method to compare the content. <br></br>
    /// Defaults to <see cref="ConflictResolution.Ignore"/> when the content equals.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="v">The constraint violation that needs to be resolved.</param>
    /// <returns></returns>
    public static ConflictResolutionAction UpdateContentOrIgnore<T>(ConstraintViolation<T> v) where T : UniqueEntity
        => UpdateContent(v, ConflictResolution.Ignore);
}