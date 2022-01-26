using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Moneyes.Data
{
    internal class RepositoryDependency<T, TDep> : IRepositoryDependency<T>
    {
        private readonly Expression<Func<T, TDep>> _dependentPropertySelectorExpression;
        private readonly Expression<Func<T, IEnumerable<TDep>>> _collectionDependentPropertySelectorExpression;

        private readonly Func<T, TDep> _dependentPropertySelector;
        private readonly Func<T, IEnumerable<TDep>> _collectionDependentPropertySelector;

        private readonly IRepositoryProvider _repositoryProvider;
        private readonly bool _hasMultipleDependents;

        public Type SourceType => typeof(TDep);
        public bool HasMultipleDependents => _hasMultipleDependents;
        public string SourceCollectionName { get; }
        public string TargetCollectionName { get; }
        public ICachedRepository<TDep> SourceRepository { get; }

        public event Action<NeedsRefresh<T>> RefreshNeeded
        {
            add
            {
                // Get the source repository of this dependency
                var sourceRepository = GetSourceRepository();

                sourceRepository.EntityAdded += SourceRepository_EntityChanged;
                sourceRepository.EntityUpdated += SourceRepository_EntityChanged;
                sourceRepository.EntityDeleted += SourceRepository_EntityChanged;

                RefreshNeededInternal += value;
            }
            remove
            {
                SourceRepository.EntityAdded -= SourceRepository_EntityChanged;
                SourceRepository.EntityUpdated -= SourceRepository_EntityChanged;
                SourceRepository.EntityDeleted -= SourceRepository_EntityChanged;

                RefreshNeededInternal -= value;
            }
        }

        private event Action<NeedsRefresh<T>> RefreshNeededInternal;

        public RepositoryDependency(IRepositoryProvider repositoryProvider,
            Expression<Func<T, TDep>> propertySelector, 
            string targetCollection, string sourceCollection = null)
            : this(repositoryProvider, targetCollection, sourceCollection)
        {
            _hasMultipleDependents = false;
            _dependentPropertySelectorExpression = propertySelector;
            _dependentPropertySelector = propertySelector.Compile();
        }
        public RepositoryDependency(IRepositoryProvider repositoryProvider,
            Expression<Func<T, IEnumerable<TDep>>> collectionPropertySelector,
            string targetCollection, string sourceCollection = null)
            : this(repositoryProvider, sourceCollection, targetCollection)
        {
            _hasMultipleDependents = true;
            _collectionDependentPropertySelectorExpression = collectionPropertySelector;
            _collectionDependentPropertySelector = collectionPropertySelector.Compile();
        }

        private RepositoryDependency(IRepositoryProvider repositoryProvider,
            string targetCollection, string sourceCollection)
        {
            SourceCollectionName = sourceCollection ?? typeof(TDep).Name;
            TargetCollectionName = targetCollection;

            _repositoryProvider = repositoryProvider;


            //// Get the source repository of this dependency
            //SourceRepository = repositoryProvider.GetRepository<TDep>(SourceCollectionName);

            //SourceRepository.EntityAdded += SourceRepository_EntityChanged;
            //SourceRepository.EntityUpdated += SourceRepository_EntityChanged;
            //SourceRepository.EntityDeleted += SourceRepository_EntityChanged;
        }

        ~RepositoryDependency()
        {
            //SourceRepository.EntityAdded -= SourceRepository_EntityChanged;
            //SourceRepository.EntityUpdated -= SourceRepository_EntityChanged;
            //SourceRepository.EntityDeleted -= SourceRepository_EntityChanged;
        }

        private ICachedRepository<TDep> GetSourceRepository()
        {
            return _repositoryProvider.GetRepository<TDep>();
        }

        private void SourceRepository_EntityChanged(TDep entity)
        {
            // Gets the key of the entity that changed in the source repo
            var changedSourceKey = SourceRepository.GetKey(entity);

            // Define the function that checks entities if they need a refresh
            bool needsRefresh(T x)
            {
                if (_hasMultipleDependents)
                {
                    // Depending collection -> Get the keys of all depending entities in the target repository
                    var dependingTargetKeys = _collectionDependentPropertySelector(x)
                        .Select(SourceRepository.GetKey);

                    // Check if the changed key is in the depending keys
                    return dependingTargetKeys.Contains(changedSourceKey);
                }
                else
                {
                    // Single dependent -> Get the key of this dependent
                    var dependingTargetKey = SourceRepository.GetKey(_dependentPropertySelector(x));

                    // Check if the key matches the changed key and the entity needs a refresh
                    return changedSourceKey.Equals(dependingTargetKey);
                }
            }

            // Invoke the refresh needed event with the function to check for refresh
            RefreshNeededInternal?.Invoke(needsRefresh);
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
    }
}
