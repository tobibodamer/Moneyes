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
        protected override bool ValidateUniqueConstaintsFor(T entity)
        {
            //var existingEntities = GetFromCache().Where(t => t.);

            //foreach (var constraint in UniqueConstraints)
            //{
            //    foreach (var existingEntity in existingEntities)
            //    {
            //        if (!constraint.Allows(entity, existingEntity))
            //        {
            //            //constraint.PropertyName
            //            //TODO: Log
            //            return false;
            //        }
            //    }
            //}

            return false;
        }
        protected override bool ValidateUniqueConstaintsFor(IEnumerable<T> entities)
        {
            var existingEntities = GetFromCache();
            var entitiesList = entities.ToList();

            foreach (var constraint in UniqueConstraints)
                foreach (var existingEntity in existingEntities)
                    foreach (var entity in entitiesList)
                        if (!constraint.Allows(entity, existingEntity))
                        {
                            //constraint.PropertyName
                            //TODO: Log
                            return false;
                        }

            return false;
        }

        #endregion
    }
}
