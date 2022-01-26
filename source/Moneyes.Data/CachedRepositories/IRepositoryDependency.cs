using LiteDB;
using System;

namespace Moneyes.Data
{
    /// <summary>
    /// Represents a dependency of one or more entity properties 
    /// from the target collection to entities from the source collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRepositoryDependency<T>
    {
        /// <summary>
        /// Gets the entity type of the source repository;
        /// </summary>
        Type SourceType { get; }

        /// <summary>
        /// Gets whether the depending property is a collection
        /// </summary>
        bool HasMultipleDependents { get; }

        /// <summary>
        /// Gets the name of the source collection.
        /// </summary>
        string SourceCollectionName { get; }

        /// <summary>
        /// Gets the name of the target collection.
        /// </summary>
        string TargetCollectionName { get; } 

        /// <summary>
        /// Raised when depending entities of the target <see cref="ICachedRepository{T}"/> 
        /// needs to be refreshed from the data source.
        /// </summary>
        event Action<NeedsRefresh<T>> RefreshNeeded;

        /// <summary>
        /// Apply this dependency to the given <see cref="ILiteCollection{T}"/> by calling the <c>Include()</c> method.
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        ILiteCollection<T> Apply(ILiteCollection<T> collection);
    }
}
