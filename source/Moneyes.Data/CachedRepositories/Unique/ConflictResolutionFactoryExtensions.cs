namespace Moneyes.Data;

public static class ConflictResolutionFactoryExtensions
{
    /// <summary>
    /// Updating an existing entity while keeping the existing id and creation timestamp.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="v"></param>
    /// <returns></returns>
    public static ConflictResolutionAction Update<T>(this ConflictResolutionFactory<T> factory)
        where T : UniqueEntity<T>
    {
        return factory.Update((old, @new) => @new with
        {
            Id = old.Id,
            CreatedAt = old.CreatedAt
        });
    }
    public static ConflictResolutionAction UpdateContent<T>(this ConflictResolutionFactory<T> factory,
        ConflictResolution defaultResolution = ConflictResolution.Ignore) where T : UniqueEntity<T>
    {
        return factory.Dynamic(violation =>
        {
            if (violation.ExistingEntity.ContentEquals(violation.NewEntity))
            {
                return defaultResolution;
            }

            return Update<T>(factory);
        });
    }

    /// <summary>
    /// Updating an existing entity when the content changed, while keeping the existing id and creation timestamp. <br></br>
    /// Uses the <see cref="UniqueEntity.ContentEquals(UniqueEntity)"/> method to compare the content. <br></br>
    /// Defaults to <see cref="ConflictResolution.Ignore"/> when the content equals.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="v">The constraint violation that needs to be resolved.</param>
    /// <returns></returns>
    public static ConflictResolutionAction UpdateContentOrIgnore<T>(this ConflictResolutionFactory<T> factory) 
        where T : UniqueEntity<T>
        => UpdateContent(factory, ConflictResolution.Ignore);
}
