using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Logging;
using Moneyes.Core;

namespace Moneyes.Data
{
    public interface IUniqueCachedRepository<T> : ICachedRepository<T, Guid>
        where T : UniqueEntity
    {
        IEnumerable<T> GetAll(bool includeSoftDeleted = false);
        T? FindById(Guid id, bool includeSoftDeleted = false);
        bool DeleteById(Guid id, bool softDelete = true);
        int DeleteAll(bool softDelete = true);
        int DeleteMany(Expression<Func<T, bool>> predicate, bool softDelete = true);
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
                return DeleteById(id);
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
            Func<ConstraintViolation, (bool continueValidation, bool ignore)> onViolation)
        {
            var validateAgainstSoftDeleted = base.CreateUniqueConstraintValidator(
                existingEntities.Where(e => e.IsDeleted), uniqueConstraints, onViolation);

            var validateAgainstNotDeleted = base.CreateUniqueConstraintValidator(
                existingEntities.Where(e => !e.IsDeleted), uniqueConstraints, onViolation);

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
}
