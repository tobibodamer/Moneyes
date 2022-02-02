using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;

namespace Moneyes.Data
{
    public class DependencyRefreshHandler
    {
        private readonly List<Entry> _entries = new();
        private class Entry
        {
            public Type SourceType { get; set; }
            public Type TargetType { get; set; }
            public string SourceName { get; set; }
            public string TargetName { get; set; }
            public object Dependency { get; set; }
            public Action<DepedencyChangedEventArgs> Callback { get; set; }
        }

        /// <summary>
        /// Gets all the entries for the given source.
        /// </summary>
        /// <typeparam name="T">The source entity type.</typeparam>
        /// <param name="sourceCollection">The collection name of the source.</param>
        /// <returns></returns>
        private IReadOnlyList<Entry> GetEntrieBySource<T>(string sourceCollection)
        {
            var entries = _entries.Where(e =>
                e.SourceType == typeof(T) &&
                e.SourceName.Equals(sourceCollection));

            return entries.ToList();
        }

        /// <summary>
        /// Notifies all registered callbacks about the change of a given entity.
        /// </summary>
        /// <typeparam name="TSource">The source entity type.</typeparam>
        /// <param name="repository">The source repository to resolve keys.</param>
        /// <param name="changedEntity">The entity that changed.</param>
        /// <param name="action">The type of change.</param>
        public void OnChangesMade<TSource>(ICachedRepository<TSource> repository, TSource changedEntity, RepositoryChangedAction action)
        {
            var entries = GetEntrieBySource<TSource>(repository.CollectionName);

            foreach (var entry in entries)
            {
                entry.Callback(new DepedencyChangedEventArgs()
                {
                    ChangedKey = repository.GetKey(changedEntity),
                    Action = action,
                    NewValue = action == RepositoryChangedAction.Remove ? null : changedEntity
                });

            }
        }
        /// <summary>
        /// Registers a callback delegate for a given <paramref name="dependency"/>.
        /// </summary>
        /// <typeparam name="T">The target entity type.</typeparam>
        /// <param name="dependency">The dependency.</param>
        /// <param name="callback">The callback that is invoked when the source repository changes.</param>
        public void RegisterCallback<T>(IRepositoryDependency<T> dependency, DependencyChangedCallback<T> callback)
        {
            ArgumentNullException.ThrowIfNull(dependency, nameof(dependency));
            ArgumentNullException.ThrowIfNull(callback, nameof(callback));

            Entry entry = new()
            {
                TargetType = typeof(T),
                SourceType = dependency.SourceType,
                TargetName = dependency.TargetCollectionName,
                SourceName = dependency.SourceCollectionName,
                Dependency = dependency,
                Callback = (args) =>
                {
                    callback(dependency, args);
                }
            };

            _entries.Add(entry);
        }

        public delegate void DependencyChangedCallback<T>(IRepositoryDependency<T> dependency, DepedencyChangedEventArgs args);

        /// <summary>
        /// Arguments associated with the change of a dependency value
        /// </summary>
        public class DepedencyChangedEventArgs
        {
            /// <summary>
            /// The key of the souce entity that changed.
            /// </summary>
            public object ChangedKey { get; init; }

            /// <summary>
            /// The type of change action.
            /// </summary>
            public RepositoryChangedAction Action { get; init; }

            /// <summary>
            /// The new value of the source entity, or <see langword="null"/> 
            /// if the action is <see cref="RepositoryChangedAction.Remove"/>.
            /// </summary>
#nullable enable
            public object? NewValue { get; init; }
#nullable disable
        }
    }
}
