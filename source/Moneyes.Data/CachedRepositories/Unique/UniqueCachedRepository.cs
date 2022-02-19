using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Logging;
using Moneyes.Core;
using Moneyes.Data;

namespace Moneyes.Data;
public interface IUniqueCachedRepository<T> : ICachedRepository<T, Guid>
    where T : UniqueEntity<T>
{
    IReadOnlyList<T> FindAllById(IEnumerable<Guid> ids, bool includeSoftDeleted = false);
#nullable enable
    T? FindById(Guid id, bool includeSoftDeleted = false);
#nullable disable

    bool ContainsAny(IEnumerable<Guid> ids, bool includeSoftDeleted = false);
    bool ContainsAll(IEnumerable<Guid> ids, bool includeSoftDeleted = false);
    bool Contains(Guid id, bool includeSoftDeleted = false);

    bool Set(T entity, bool keepCreationTimestamp = true);
    bool Set(T entity, ConflictResolutionDelegate<T> onConflict, bool keepCreationTimestamp = true);
    int SetMany(IEnumerable<T> entities, bool keepCreationTimestamp = true);
    int SetMany(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict, bool keepCreationTimestamp = true);

    bool Update(T entity, bool keepCreationTimestamp = true);
    bool Update(T entity, ConflictResolutionDelegate<T> onConflict, bool keepCreationTimestamp = true);
    int UpdateMany(IEnumerable<T> entities, bool keepCreationTimestamp = true);
    int UpdateMany(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict, bool keepCreationTimestamp = true);

    bool DeleteById(Guid id, bool softDelete = true);
    int DeleteAll(bool softDelete = true);
    int DeleteMany(Expression<Func<T, bool>> predicate, bool softDelete = true);

    IEnumerable<T> IncludeSoftDeleted();
}

public class UniqueCachedRepository<T> : CachedRepository<T, Guid>, IUniqueCachedRepository<T>
    where T : UniqueEntity<T>
{
    public UniqueCachedRepository(
        IDatabaseProvider<ILiteDatabase> databaseProvider,
        Func<T, Guid> keySelector,
        CachedRepositoryOptions options,
        DependencyRefreshHandler refreshHandler,
        IEnumerable<IRepositoryDependency<T>> repositoryDependencies = null,
        IEnumerable<IUniqueConstraint<T>> uniqueConstraints = null,
        ILogger<UniqueCachedRepository<T>> logger = null)
        : base(databaseProvider, options, refreshHandler, keySelector, repositoryDependencies, uniqueConstraints, logger)
    {
    }
    protected override T PostQueryTransform(T entity)
    {
        // Dont include soft deleted dependents
        foreach (var dependency in RepositoryDependencies)
        {
            var softDeletedDependents = dependency.GetDependentsOf(entity)
                .OfType<T>()
                .Where(x => x.IsDeleted)
                .Select(x => x.Id as object)
                .ToArray();

            if (softDeletedDependents.Length == 0)
            {
                continue;
            }

            dependency.RemoveDependents(entity, softDeletedDependents);
        }

        return entity;
    }

    /// <summary>
    /// A unique property index that encapsulates two sepereate indices for soft deleted / not soft deleted entities.
    /// </summary>
    private class SoftDeleteUniqueIndex : IUniqueIndex<T, Guid>
    {
        private readonly IUniqueIndex<T, Guid> _index;
        private readonly IUniqueIndex<T, Guid> _deletedIndex;

        public IUniqueConstraint<T> Constraint => _index.Constraint;

        public SoftDeleteUniqueIndex(IUniqueConstraint<T> uniqueConstraint)
        {
            _index = new UniqueIndex<T, Guid>(uniqueConstraint);
            _deletedIndex = new UniqueIndex<T, Guid>(uniqueConstraint);
        }

        public SoftDeleteUniqueIndex(IUniqueConstraint<T> uniqueConstraint, IEnumerable<KeyValuePair<Guid, T>> entities)
        {
            _index = new UniqueIndex<T, Guid>(uniqueConstraint, entities);
            _deletedIndex = new UniqueIndex<T, Guid>(uniqueConstraint, entities);
        }

        private SoftDeleteUniqueIndex(IUniqueIndex<T, Guid> index, IUniqueIndex<T, Guid> deletedIndex)
        {
            _index = index;
            _deletedIndex = deletedIndex;
        }

        public IUniqueIndex<T, Guid> Copy()
        {
            return new SoftDeleteUniqueIndex(_index.Copy(), _deletedIndex.Copy());
        }

        public bool GetOrAddEntity(Guid key, T entity, out Guid existingKey)
        {
            if (entity.IsDeleted)
            {
                return _deletedIndex.GetOrAddEntity(key, entity, out existingKey);
            }

            return _index.GetOrAddEntity(key, entity, out existingKey);
        }

        public void RemoveEntity(Guid key)
        {
            _index.RemoveEntity(key);
            _deletedIndex.RemoveEntity(key);
        }

        public bool TryGetExistingEntity(T entity, out Guid existingKey)
        {
            if (entity.IsDeleted)
            {
                return _deletedIndex.TryGetExistingEntity(entity, out existingKey);
            }

            return _index.TryGetExistingEntity(entity, out existingKey);
        }

        public void UpdateEntity(Guid key, T entity)
        {
            if (entity.IsDeleted)
            {
                _deletedIndex.UpdateEntity(key, entity);
                return;
            }

            _index.UpdateEntity(key, entity);
        }
    }

#nullable enable
    protected override IUniqueIndex<T, Guid> CreateUniqueIndex(IUniqueConstraint<T> uniqueConstraint,
        IReadOnlyList<KeyValuePair<Guid, T>>? entities)
#nullable disable
    {
        if (entities is null)
        {
            return new SoftDeleteUniqueIndex(uniqueConstraint);
        }

        return new SoftDeleteUniqueIndex(uniqueConstraint, entities);
    }

    protected override UniqueConstraintValidator CreateUniqueConstraintValidator(
        IEnumerable<IUniqueIndex<T, Guid>> uniqueIndices,
        Func<IUniqueConstraint<T>, IUniqueIndex<T, Guid>> uniqueIndexFactory,
        Func<ConstraintViolation<T>, (bool continueValidation, bool ignoreViolation)> onViolation,
        Action<Guid, T, bool> onFinished)
    {
        return base.CreateUniqueConstraintValidator(
            uniqueIndices,
            uniqueIndexFactory,
            onViolation: violation =>
            {
                if (violation.ExistingEntity.IsDeleted)
                {
                    // We ignore violations for entities that are already soft deleted.
                    // Unique constraints do not apply to deleted entities,
                    // but they will if the deleted entity is restored.
                    return (continueValidation: true, ignoreViolation: true);
                }

                return onViolation(violation);
            },
            onFinished);
    }
    public override IEnumerable<T> GetAll()
    {
        return base.GetAll().Where(x => !x.IsDeleted);
    }

    public IEnumerable<T> IncludeSoftDeleted()
    {
        return base.GetAll();
    }

    public override T FindById(Guid id)
    {
        return FindById(id, includeSoftDeleted: true);
    }

    public T FindById(Guid id, bool includeSoftDeleted = true)
    {
        var result = base.FindById(id);

        if (!includeSoftDeleted && (result?.IsDeleted ?? true))
        {
            return null;
        }

        return result;
    }

    public IReadOnlyList<T> FindAllById(IEnumerable<Guid> ids, bool includeSoftDeleted = true)
    {
        if (includeSoftDeleted)
        {
            return base.FindAllById(ids);
        }

        ArgumentNullException.ThrowIfNull(ids);

        if (!ids.Any())
        {
            return new List<T>();
        }

        return ids
            .Select(id => Cache.GetValueOrDefault(id))
            .Where(x => !x?.IsDeleted ?? false)
            .ToList();
    }

    public override IReadOnlyList<T> FindAllById(IEnumerable<Guid> ids)
    {
        return base.FindAllById(ids);
    }

    public bool Contains(Guid id, bool includeSoftDeleted = true)
    {
        if (includeSoftDeleted)
        {
            return base.Contains(id);
        }

        if (Cache.TryGetValue(id, out var entity))
        {
            return !entity.IsDeleted;
        }

        return false;
    }

    public override bool Contains(Guid id)
    {
        return Contains(id, includeSoftDeleted: true);
    }

    public bool ContainsAll(IEnumerable<Guid> ids, bool includeSoftDeleted = true)
    {
        if (includeSoftDeleted)
        {
            return base.ContainsAll(ids);
        }

        return ids.All(id =>
        {
            if (Cache.TryGetValue(id, out var entity))
            {
                return !entity.IsDeleted;
            }

            return false;
        });
    }

    public override bool ContainsAll(IEnumerable<Guid> ids)
    {
        return ContainsAll(ids, includeSoftDeleted: true);
    }

    public bool ContainsAny(IEnumerable<Guid> ids, bool includeSoftDeleted = true)
    {
        if (includeSoftDeleted)
        {
            return base.ContainsAny(ids);
        }

        return ids.Any(id =>
        {
            if (Cache.TryGetValue(id, out var entity))
            {
                return !entity.IsDeleted;
            }

            return false;
        });
    }

    public int SetMany(IEnumerable<T> entities, bool keepCreationTimestamp = true)
    {
        if (keepCreationTimestamp)
        {
            return SetMany(entities, addEntityFactory: AddEntityFactory, updateEntityFactory: UpdateEntityFactory);
        }

        return base.SetMany(entities);
    }
    public override int SetMany(IEnumerable<T> entities)
    {
        return SetMany(entities, keepCreationTimestamp: true);
    }

    public int SetMany(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict, bool keepCreationTimestamp = true)
    {
        if (keepCreationTimestamp)
        {
            return SetMany(entities, addEntityFactory: AddEntityFactory, updateEntityFactory: UpdateEntityFactory, onConflict);
        }

        return base.SetMany(entities, onConflict);
    }

    public override int SetMany(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict)
    {
        return SetMany(entities, onConflict, keepCreationTimestamp: true);
    }

    public bool Set(T entity, bool keepCreationTimestamp = true)
    {
        if (keepCreationTimestamp)
        {
            return Set(entity, addEntityFactory: AddEntityFactory, updateEntityFactory: UpdateEntityFactory);
        }

        return base.Set(entity);
    }

    public override bool Set(T entity)
    {
        return Set(entity, keepCreationTimestamp: true);
    }

    public bool Set(T entity, ConflictResolutionDelegate<T> onConflict, bool keepCreationTimestamp = true)
    {
        if (keepCreationTimestamp)
        {
            return Set(entity, addEntityFactory: AddEntityFactory, updateEntityFactory: UpdateEntityFactory, onConflict);
        }

        return base.Set(entity, onConflict);
    }

    public override bool Set(T entity, ConflictResolutionDelegate<T> onConflict)
    {
        return Set(entity, onConflict, keepCreationTimestamp: true);
    }

    public int UpdateMany(IEnumerable<T> entities, bool keepCreationTimestamp = true)
    {
        if (keepCreationTimestamp)
        {
            return UpdateMany(entities, updateEntityFactory: UpdateEntityFactory);
        }

        return base.UpdateMany(entities);
    }
    public override int UpdateMany(IEnumerable<T> entities)
    {
        return UpdateMany(entities, keepCreationTimestamp: true);
    }

    public int UpdateMany(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict, bool keepCreationTimestamp = true)
    {
        if (keepCreationTimestamp)
        {
            return UpdateMany(entities, updateEntityFactory: UpdateEntityFactory, onConflict);
        }

        return base.UpdateMany(entities, onConflict);
    }

    public override int UpdateMany(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict)
    {
        return UpdateMany(entities, onConflict, keepCreationTimestamp: true);
    }

    public bool Update(T entity, bool keepCreationTimestamp = true)
    {
        if (keepCreationTimestamp)
        {
            return Update(entity, updateEntityFactory: UpdateEntityFactory);
        }

        return base.Update(entity);
    }

    public override bool Update(T entity)
    {
        return Update(entity, keepCreationTimestamp: true);
    }

    public bool Update(T entity, ConflictResolutionDelegate<T> onConflict, bool keepCreationTimestamp = true)
    {
        if (keepCreationTimestamp)
        {
            return Update(entity, updateEntityFactory: UpdateEntityFactory, onConflict);
        }

        return base.Update(entity, onConflict);
    }

    public override bool Update(T entity, ConflictResolutionDelegate<T> onConflict)
    {
        return Update(entity, onConflict, keepCreationTimestamp: true);
    }

    protected virtual T AddEntityFactory(T newEntity)
    {
        return newEntity;
    }
    protected virtual T UpdateEntityFactory(T existingEntity, T newEntity)
    {
        return newEntity with { CreatedAt = existingEntity.CreatedAt };
    }

    /// <summary>
    /// Soft deletes an entity by its id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public override bool DeleteById(Guid id)
    {
        return DeleteById(id, softDelete: true);
    }

    public bool DeleteById(Guid id, bool softDelete = true)
    {
        if (softDelete)
        {
            if (Cache.TryGetValue(id, out T entity))
            {
                //entity.IsDeleted = true;

                var deletedEntity = entity with { IsDeleted = true };

                base.Update(deletedEntity);

                return true;
            }

            return false;
        }

        return base.DeleteById(id);
    }
    public override int DeleteAll()
    {
        return DeleteAll(softDelete: true);
    }
    public int DeleteAll(bool softDelete = true)
    {
        if (softDelete)
        {
            var entitiesDeleted = GetAll().Select(entity =>
                entity with { IsDeleted = true });

            return base.UpdateMany(entitiesDeleted);
        }

        return base.DeleteAll();
    }

    public override int DeleteMany(Expression<Func<T, bool>> predicate)
    {
        var compiledPredicate = predicate.Compile();

        var entities = GetAll()
            .Where(compiledPredicate)
            .Select(entity => entity with { IsDeleted = true });

        return base.UpdateMany(entities);
    }

    public int DeleteMany(Expression<Func<T, bool>> predicate, bool softDelete = true)
    {
        if (softDelete)
        {
            return DeleteMany(predicate);
        }

        return base.DeleteMany(predicate);
    }

    public override int DeleteMany(IReadOnlySet<Guid> keys)
    {
        var softDeletedEntities = base.FindAllById(keys)
            .Select(entity => entity with { IsDeleted = true });

        return base.UpdateMany(softDeletedEntities);
    }
    protected override void OnEntityUpdated(T entity, bool notifyDependencyHandler)
    {
        if (entity.IsDeleted == true)
        {
            if (notifyDependencyHandler)
            {
                DependencyRefreshHandler.OnChangesMade(this, entity, RepositoryChangedAction.Replace);
            }

            base.OnEntityDeleted(entity, false);
        }

        base.OnEntityUpdated(entity, notifyDependencyHandler);
    }

}