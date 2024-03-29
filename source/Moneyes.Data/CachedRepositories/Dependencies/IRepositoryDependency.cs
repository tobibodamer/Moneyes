﻿using LiteDB;
using System;
using System.Collections;

namespace Moneyes.Data
{
    public interface IRepositoryDependency
    {
        /// <summary>
        /// Gets the entity type of the source repository;
        /// </summary>
        Type SourceType { get; }

        /// <summary>
        /// Gets the entity type of the target repository;
        /// </summary>
        Type TargetType { get; }

        /// <summary>
        /// Gets the property name of the dependent.
        /// </summary>
        string PropertyName { get; }

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
    }

    /// <summary>
    /// Represents a dependency of one or more entity properties 
    /// from the target collection to entities from the source collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRepositoryDependency<T> : IRepositoryDependency
    {
        /// <summary>
        /// Apply this dependency to the given <see cref="ILiteCollection{T}"/> by calling the <c>Include()</c> method.
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        ILiteCollection<T> Apply(ILiteCollection<T> collection);

        /// <summary>
        /// Check whether a given <paramref name="entity"/> is affected by the change of a depending entity,
        /// and might need a refresh.
        /// </summary>
        /// <param name="changedSourceKey">The key of the changed source entity.</param>
        /// <param name="entity">The target entity to check.</param>
        /// <returns></returns>
        bool NeedsRefresh(object changedSourceKey, T entity);

        /// <summary>
        /// Replaces the dependent with the given key of an <paramref name="entity"/> 
        /// with <paramref name="newValue"/>.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <param name="sourceKeyToReplace">The key of the dependent to replace.</param>
        /// <param name="newValue">The new dependent value.</param>
        void ReplaceDependent(T entity, object sourceKeyToReplace, object newValue);

        /// <summary>
        /// Removes / nulls all dependent properties of a given <paramref name="entity"/> with the given <paramref name="keys"/>.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <param name="keys">The keys of the dependents to remove.</param>
        void RemoveDependents(T entity, params object[] keys);

        /// <summary>
        /// Gets all dependents of a given <paramref name="entity"/>.
        /// Will contain a single value if this dependency has only one dependent.
        /// </summary>
        /// <param name="entity">The target entity.</param>
        /// <returns>A <see cref="IEnumerable"/> of all dependents.</returns>
        IEnumerable GetDependentsOf(T entity);
    }
}
