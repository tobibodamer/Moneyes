using LiteDB;
using Moneyes.Core.Filters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace Moneyes.Data
{
    internal class RepositoryDependency<T, TDep> : IRepositoryDependency<T>
        where TDep : class
    {
        private readonly Expression<Func<T, TDep>> _dependentPropertySelectorExpression;
        private readonly Expression<Func<T, ICollection<TDep>>> _collectionDependentPropertySelectorExpression;

        private readonly Func<T, TDep> _dependentPropertySelector;
        private readonly Func<T, ICollection<TDep>> _collectionDependentPropertySelector;

        private readonly IRepositoryProvider _repositoryProvider;
        private readonly bool _hasMultipleDependents;

        private readonly Lazy<IEnumerable<IRepositoryDependency<TDep>>> _transitiveDepsLazy;

        public Type SourceType => typeof(TDep);
        public Type TargetType => typeof(T);

        public string SourceCollectionName { get; }
        public string TargetCollectionName { get; }

        public bool HasMultipleDependents => _hasMultipleDependents;

        public string PropertyName { get; }

        /// <summary>
        /// Gets the direct transitive dependencies of the source repository.
        /// </summary>
        public IEnumerable<IRepositoryDependency<TDep>> TransitiveDependencies => _transitiveDepsLazy.Value;

        public RepositoryDependency(
            IRepositoryProvider repositoryProvider,
            Expression<Func<T, TDep>> propertySelector,
            string targetCollection, 
            string sourceCollection,
            Func<IEnumerable<IRepositoryDependency<TDep>>> getTransitiveDependencies)
            : this(repositoryProvider, targetCollection, sourceCollection, getTransitiveDependencies)
        {
            _hasMultipleDependents = false;
            _dependentPropertySelectorExpression = propertySelector;
            _dependentPropertySelector = propertySelector.Compile();

            PropertyName = FilterExtensions.GetName(propertySelector);
        }
        public RepositoryDependency(
            IRepositoryProvider repositoryProvider,
            Expression<Func<T, ICollection<TDep>>> collectionPropertySelector,
            string targetCollection, 
            string sourceCollection,
            Func<IEnumerable<IRepositoryDependency<TDep>>> getTransitiveDependencies)
            : this(repositoryProvider, targetCollection, sourceCollection, getTransitiveDependencies)
        {
            _hasMultipleDependents = true;
            _collectionDependentPropertySelectorExpression = collectionPropertySelector;
            _collectionDependentPropertySelector = collectionPropertySelector.Compile();

            PropertyName = FilterExtensions.GetName(collectionPropertySelector);
        }

        private RepositoryDependency(IRepositoryProvider repositoryProvider,
            string targetCollection, string sourceCollection,
            Func<IEnumerable<IRepositoryDependency<TDep>>> getTransitiveDependencies)
        {
            SourceCollectionName = sourceCollection;
            TargetCollectionName = targetCollection;

            _repositoryProvider = repositoryProvider;
            _transitiveDepsLazy = new(getTransitiveDependencies);
        }

        public bool NeedsRefresh(object changedSourceKey, T entityToCheck)
        {
            ArgumentNullException.ThrowIfNull(changedSourceKey);
            ArgumentNullException.ThrowIfNull(entityToCheck);

            var sourceRepository = _repositoryProvider.GetRepository<TDep>(SourceCollectionName);

            if (_hasMultipleDependents)
            {
                // Depending collection -> Get the keys of all depending entities in the target repository
                var dependingTargetKeys = _collectionDependentPropertySelector(entityToCheck)
                    .Where(p => p != null)
                    .Select(sourceRepository.GetKey);

                // Check if the changed key is in the depending keys
                return dependingTargetKeys.Contains(changedSourceKey);
            }
            else
            {
                var dependentProperty = _dependentPropertySelector(entityToCheck);

                if (dependentProperty == null)
                {
                    return false;
                }

                // Single dependent -> Get the key of this dependent
                var dependingTargetKey = sourceRepository.GetKey(dependentProperty);

                // Check if the key matches the changed key and the entity needs a refresh
                return changedSourceKey.Equals(dependingTargetKey);
            }
        }

        private void SetDependentProperty(T targetEntity, TDep value)
        {
            if (!HasMultipleDependents)
            {
                ParameterExpression valueParameterExpression = Expression.Parameter(typeof(object));
                Expression targetExpression = _dependentPropertySelectorExpression.Body;

                var newValue = Expression.Parameter(_dependentPropertySelectorExpression.Body.Type);
                var assign = Expression.Lambda<Action<T, TDep>>
                            (
                                Expression.Assign(targetExpression, Expression.Convert(valueParameterExpression, targetExpression.Type)),
                                _dependentPropertySelectorExpression.Parameters.Single(),
                                valueParameterExpression
                            );

                assign.Compile().Invoke(targetEntity, value);
            }
        }

        public void RemoveDependents(T entity, params object[] keys)
        {
            //if (sourceRepository is not ICachedRepository<TDep> sourceRepositoryCasted)
            //{
            //    throw new ArgumentException("Invalid source repository type.", nameof(sourceRepository));
            //}

            var sourceRepository = _repositoryProvider.GetRepository<TDep>(SourceCollectionName);

            if (_hasMultipleDependents)
            {
                // Depending collection -> Get the keys of all depending entities in the target repository
                var depending = _collectionDependentPropertySelector(entity);

                foreach (var depEntity in depending.ToList())
                {
                    if (depEntity == null ||
                        !keys.Contains(sourceRepository.GetKey(depEntity)))
                    {
                        continue;
                    }

                    depending.Remove(depEntity);
                }
            }
            else
            {
                var dependentProperty = _dependentPropertySelector(entity);

                if (dependentProperty == null || !keys.Contains(sourceRepository.GetKey(dependentProperty)))
                {
                    // Set null
                    SetDependentProperty(entity, default);
                }
            }
        }

        public void ReplaceDependency(object sourceKeyToReplace, T entity, object newValue)
        {
            if (newValue is not TDep updateValue)
            {
                throw new ArgumentException(null, nameof(newValue));
            }

            var sourceRepository = _repositoryProvider.GetRepository<TDep>(SourceCollectionName);

            if (_hasMultipleDependents)
            {
                // Depending collection -> Get the keys of all depending entities in the target repository
                var depending = _collectionDependentPropertySelector(entity);

                foreach (var depEntity in depending.ToList())
                {
                    if (depEntity == null ||
                        !sourceRepository.GetKey(depEntity).Equals(sourceKeyToReplace))
                    {
                        continue;
                    }

                    depending.Remove(depEntity);
                    depending.Add(updateValue);
                }
            }
            else
            {
                var dependentProperty = _dependentPropertySelector(entity);

                if (sourceKeyToReplace.Equals(sourceRepository.GetKey(dependentProperty)))
                {
                    // Set null
                    SetDependentProperty(entity, updateValue);
                }
            }
        }

        public void UpdateDependency(T entity, DependencyRefreshHandler.DepedencyChangedEventArgs e)
        {
            if (e.Action != RepositoryChangedAction.Remove && e.NewValue is not TDep)
            {
                throw new ArgumentException("New value must be of source entity type.", nameof(e));
            }

            TDep newValue = (TDep)e.NewValue;

            var sourceRepository = _repositoryProvider.GetRepository<TDep>(SourceCollectionName);

            if (_hasMultipleDependents)
            {
                // Depending collection -> Get the keys of all depending entities in the target repository
                var depending = _collectionDependentPropertySelector(entity);

                foreach (var depEntity in depending.ToList())
                {
                    // Match key
                    if (depEntity == null ||
                        !sourceRepository.GetKey(depEntity).Equals(e.ChangedKey))
                    {
                        continue;
                    }

                    // Update collection

                    if (e.Action is RepositoryChangedAction.Remove or RepositoryChangedAction.Replace)
                    {
                        depending.Remove(depEntity);
                    }

                    if (e.Action is not RepositoryChangedAction.Remove)
                    {
                        depending.Add(newValue);
                    }
                }
            }
            else
            {
                var depEntity = _dependentPropertySelector(entity);

                // Match key
                if (depEntity == null ||
                    !sourceRepository.GetKey(depEntity).Equals(e.ChangedKey))
                {
                    return;
                }

                // Update value

                if (e.Action is RepositoryChangedAction.Add or RepositoryChangedAction.Replace)
                {
                    SetDependentProperty(entity, newValue);
                }
                else if (e.Action is RepositoryChangedAction.Remove)
                {
                    SetDependentProperty(entity, null);
                }
            }
        }

        public ILiteCollection<T> Apply(ILiteCollection<T> collection)
        {
            if (HasMultipleDependents)
            {
                return collection.Include(_collectionDependentPropertySelectorExpression);
            }
            else
            {
                return collection.Include(_dependentPropertySelectorExpression);
            }
        }

        public IEnumerable GetDependentsOf(T entity)
        {
            if (HasMultipleDependents)
            {
                var depEntities = _collectionDependentPropertySelector(entity);

                if (depEntities is null)
                {
                    yield break;
                }

                foreach (var depEntity in depEntities)
                {
                    yield return depEntity;
                }
            }
            else
            {
                var depEntity = _dependentPropertySelector(entity);

                if (depEntity is null)
                {
                    yield break;
                }

                yield return depEntity;
            }
        }
    }
}
