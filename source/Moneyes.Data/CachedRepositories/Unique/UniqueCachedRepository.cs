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
    where T : UniqueEntity
{
    IEnumerable<T> GetAll(bool includeSoftDeleted = false);
    T? FindById(Guid id, bool includeSoftDeleted = false);
    bool DeleteById(Guid id, bool softDelete = true);
    int DeleteAll(bool softDelete = true);
    int DeleteMany(Expression<Func<T, bool>> predicate, bool softDelete = true);
    bool ContainsAny(bool includeSoftDeleted = false, params object[] ids);
    bool ContainsAll(bool includeSoftDeleted = false, params object[] ids);
    bool Contains(object id, bool includeSoftDeleted = false);
    IReadOnlyList<T> FindAllById(bool includeSoftDeleted = false, params object[] ids);
    int Set(IEnumerable<T> entities, bool keepCreationTimestamp = true);
    int Set(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict, bool keepCreationTimestamp = true);
    bool Set(T entity, bool keepCreationTimestamp = true);
    bool Set(T entity, ConflictResolutionDelegate<T> onConflict, bool keepCreationTimestamp = true);
    int Update(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict, bool keepCreationTimestamp = true);
    bool Update(T entity, bool keepCreationTimestamp = true);
    bool Update(T entity, ConflictResolutionDelegate<T> onConflict, bool keepCreationTimestamp = true);
    int Update(IEnumerable<T> entities, bool keepCreationTimestamp = true);
}

public class UniqueCachedRepository<T> : CachedRepository<T, Guid>, IUniqueCachedRepository<T>
    where T : UniqueEntity
{
    public UniqueCachedRepository(
        IDatabaseProvider<ILiteDatabase> databaseProvider,
        Func<T, Guid> keySelector,
        CachedRepositoryOptions options,
        DependencyRefreshHandler refreshHandler,
        bool autoId = false,
        IEnumerable<IRepositoryDependency<T>> repositoryDependencies = null,
        IEnumerable<IUniqueConstraint<T>> uniqueConstraints = null,
        ILogger<UniqueCachedRepository<T>> logger = null)
        : base(databaseProvider, keySelector, options, refreshHandler, autoId, repositoryDependencies, uniqueConstraints, logger)
    {
    }

    protected override T PostQueryTransform(T entity)
    {
        // Dont include soft deleted dependents
        foreach (var dependency in RepositoryDependencies)
        {
            var softDeletedDependents = dependency.GetDependentsOf(entity)
                .OfType<UniqueEntity>()
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

    public override IEnumerable<T> GetAll()
    {
        return GetAll(includeSoftDeleted: false);
    }
    public IEnumerable<T> GetAll(bool includeSoftDeleted = false)
    {
        if (!includeSoftDeleted)
        {
            return base.GetAll().Where(x => !x.IsDeleted);
        }

        return base.GetAll();
    }

    public override T FindById(Guid id)
    {
        return FindById(id, includeSoftDeleted: false);
    }

    public T FindById(Guid id, bool includeSoftDeleted = false)
    {
        var result = base.FindById(id);

        if (includeSoftDeleted || (result?.IsDeleted ?? true))
        {
            return null;
        }

        return result;
    }

    public IReadOnlyList<T> FindAllById(bool includeSoftDeleted = false, params object[] ids)
    {
        if (includeSoftDeleted)
        {
            return base.FindAllById(ids);
        }

        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Length == 0)
        {
            return new List<T>();
        }

        return ids
            .Select(id => Cache.GetValueOrDefault(id))
            .Where(x => !x?.IsDeleted ?? false)
            .ToList();
    }

    public override IReadOnlyList<T> FindAllById(params object[] ids)
    {
        return base.FindAllById(ids);
    }

    public bool Contains(object id, bool includeSoftDeleted = false)
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

    public override bool Contains(object id)
    {
        return Contains(id, includeSoftDeleted: false);
    }

    public bool ContainsAll(bool includeSoftDeleted = false, params object[] ids)
    {
        if (includeSoftDeleted)
        {
            return base.Contains(ids);
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

    public override bool ContainsAll(params object[] ids)
    {
        return ContainsAll(false, ids);
    }

    public bool ContainsAny(bool includeSoftDeleted = false, params object[] ids)
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

    public int Set(IEnumerable<T> entities, bool keepCreationTimestamp = true)
    {
        if (keepCreationTimestamp)
        {
            return Set(entities, addEntityFactory: AddEntityFactory, updateEntityFactory: UpdateEntityFactory);
        }

        return base.Set(entities);
    }
    public override int Set(IEnumerable<T> entities)
    {
        return Set(entities, keepCreationTimestamp: true);
    }

    public int Set(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict, bool keepCreationTimestamp = true)
    {
        if (keepCreationTimestamp)
        {
            return Set(entities, addEntityFactory: AddEntityFactory, updateEntityFactory: UpdateEntityFactory, onConflict);
        }

        return base.Set(entities, onConflict);
    }

    public override int Set(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict)
    {
        return Set(entities, onConflict, keepCreationTimestamp: true);
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

    public int Update(IEnumerable<T> entities, bool keepCreationTimestamp = true)
    {
        if (keepCreationTimestamp)
        {
            return Update(entities, updateEntityFactory: UpdateEntityFactory);
        }

        return base.Update(entities);
    }
    public override int Update(IEnumerable<T> entities)
    {
        return Update(entities, keepCreationTimestamp: true);
    }

    public int Update(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict, bool keepCreationTimestamp = true)
    {
        if (keepCreationTimestamp)
        {
            return Update(entities, updateEntityFactory: UpdateEntityFactory, onConflict);
        }

        return base.Update(entities, onConflict);
    }

    public override int Update(IEnumerable<T> entities, ConflictResolutionDelegate<T> onConflict)
    {
        return Update(entities, onConflict, keepCreationTimestamp: true);
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
        newEntity.CreatedAt = existingEntity.CreatedAt;

        return newEntity;
    }

    /// <summary>
    /// Soft deletes an entity by its id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public override bool DeleteById(object id)
    {
        if (Cache.TryGetValue(id, out T entity))
        {
            entity.IsDeleted = true;

            Update(entity);

            return true;
        }

        return false;
    }

    public bool DeleteById(Guid id, bool softDelete = true)
    {
        if (softDelete)
        {
            return base.DeleteById(id);
        }

        return base.DeleteById(id);
    }
    public override int DeleteAll()
    {
        var entities = GetAll(includeSoftDeleted: false).ToList();

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
        }

        return Update(entities);
    }
    public int DeleteAll(bool softDelete = true)
    {
        if (softDelete)
        {
            return DeleteAll();
        }

        return base.DeleteAll();
    }

    public override int DeleteMany(Expression<Func<T, bool>> predicate)
    {
        var compiledPredicate = predicate.Compile();

        var entities = GetAll(includeSoftDeleted: false)
            .Where(compiledPredicate)
            .ToList();

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
        }

        return Update(entities);
    }

    public int DeleteMany(Expression<Func<T, bool>> predicate, bool softDelete = true)
    {
        if (softDelete)
        {
            return DeleteMany(predicate);
        }

        return base.DeleteMany(predicate);
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

    #region Validation

    protected override Func<T, bool> CreateUniqueConstraintValidator(
        IEnumerable<T> existingEntities,
        IEnumerable<IUniqueConstraint<T>> uniqueConstraints,
        Func<ConstraintViolation<T>, (bool continueValidation, bool ignore)> onViolation)
    {
        var existingEntitiesCopy = existingEntities.ToList();

        var validateAgainstSoftDeleted = base.CreateUniqueConstraintValidator(
            existingEntitiesCopy.Where(e => e.IsDeleted), uniqueConstraints, onViolation);

        var validateAgainstNotDeleted = base.CreateUniqueConstraintValidator(
            existingEntitiesCopy.Where(e => !e.IsDeleted), uniqueConstraints, onViolation);

        return (entity) =>
        {
            // return the specific validation function, based on if the entity is deleted
            if (entity.IsDeleted)
            {
                return validateAgainstSoftDeleted(entity);
            }

            return validateAgainstNotDeleted(entity);
        };
    }

    #endregion
}