using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moneyes.Core;

namespace Moneyes.Data
{
    public interface IUniqueCachedRepository<T> : ICachedRepository<T, Guid>
        where T: UniqueEntity
    {
    }

    public class UniqueCachedRepository<T> : CachedRepository<T, Guid>, IUniqueCachedRepository<T>
        where T : UniqueEntity
    {
        public UniqueCachedRepository(
            IDatabaseProvider databaseProvider, 
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


        #region Validation

        /// <summary>
        /// Validate unique constraints for a entity that is not soft deleted.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected override bool ValidateUniqueConstaintsFor(T entity)
        {            
            return entity.IsDeleted || base.ValidateUniqueConstaintsFor(entity);
        }

        /// <summary>
        /// Validate the unique constraints for all entites that are not soft deleted.
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        protected override bool ValidateUniqueConstaintsFor(IEnumerable<T> entities)
        {
            var notSoftDeletedEntities = entities.Where(e => !e.IsDeleted);

            return base.ValidateUniqueConstaintsFor(notSoftDeletedEntities);
        }

        #endregion
    }
}
