using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moneyes.Core;
using Microsoft.Extensions.Logging;
using LiteDB;

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
            var databaseProvider = ServiceProvider.GetRequiredService<IDatabaseProvider<ILiteDatabase>>();
            var refreshHandler = ServiceProvider.GetService<DependencyRefreshHandler>();
            var logger = ServiceProvider.GetService<ILogger<UniqueCachedRepository<T>>>();

            return new UniqueCachedRepository<T>(
                databaseProvider, keySelector, options, refreshHandler, autoId, repositoryDependencies, uniqueConstraints, logger);
        }

        public override ICachedRepository<T> CreateRepository(CachedRepositoryOptions options, bool autoId)
        {
            string collectionName = options.CollectionName;

            var repositoryDependencies = GetRepositoryDependencies(collectionName);
            var uniqueConstraints = GetUniqueConstraints(collectionName);
            var databaseProvider = ServiceProvider.GetRequiredService<IDatabaseProvider<ILiteDatabase>>();
            var refreshHandler = ServiceProvider.GetService<DependencyRefreshHandler>();
            var logger = ServiceProvider.GetService<ILogger<UniqueCachedRepository<T>>>();

            return new UniqueCachedRepository<T>(databaseProvider, x => x.Id, options, refreshHandler, autoId, repositoryDependencies, uniqueConstraints, logger);
        }
    }
}
