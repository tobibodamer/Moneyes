using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moneyes.Core;

namespace Moneyes.Data
{
    public class UniqueCachedRepositoryFactory<T> : CachedRepositoryFactory<T, Guid>
        where T : UniqueEntity
    {
        public UniqueCachedRepositoryFactory(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override ICachedRepository<T, Guid> CreateRepository(CachedRepositoryOptions options, Func<T, Guid> keySelector, bool autoId)
        {
            string collectionName = options.CollectionName;

            var repositoryDependencies = GetRepositoryDependencies(collectionName);
            var uniqueConstraints = GetUniqueConstraints(collectionName);
            var databaseProvider = ServiceProvider.GetRequiredService<IDatabaseProvider>();

            return new UniqueCachedRepository<T>(
                databaseProvider, options, keySelector, autoId, repositoryDependencies, uniqueConstraints);
        }

        public override ICachedRepository<T> CreateRepository(CachedRepositoryOptions options, bool autoId)
        {
            string collectionName = options.CollectionName;

            var repositoryDependencies = GetRepositoryDependencies(collectionName);
            var uniqueConstraints = GetUniqueConstraints(collectionName);
            var databaseProvider = ServiceProvider.GetRequiredService<IDatabaseProvider>();

            return new UniqueCachedRepository<T>(databaseProvider, options, null, autoId, repositoryDependencies, uniqueConstraints);
        }
    }
}
