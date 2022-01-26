using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Moneyes.Data
{
    public class CachedRepositoriesOptions
    {
        private readonly LiteDbBuilder _builder;
        private readonly BsonMapper _bsonMapper = new();
        private readonly List<RepositoryEntry> _repositoryEntries = new();
        internal CachedRepositoriesOptions(LiteDbBuilder liteDbBuilder)
        {
            _builder = liteDbBuilder;

            _builder.Services.AddScoped<BsonMapperFactory>(p =>
            {
                return new(() => _bsonMapper);
            });
        }

        private struct RepositoryEntry
        {
            public Type EntityType { get; init; }
            public Type RepositoryType { get; init; }
            public Action<IServiceCollection> RegisterGenericRepository { get; init; }
            public CachedRepositoryOptions Options { get; init; }
        }

        /// <summary>
        /// Adds a repository for the given entity type.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="collectionName">Custom table name.</param>
        /// <returns>An instance of <see cref="CachedRepositoryBuilder{T}"/> to build the repository.</returns>
        public CachedRepositoryBuilder<T> AddRepository<T>(string collectionName)
        {
            return AddRepository<T>(options => options.CollectionName = collectionName);
        }

        /// <summary>
        /// Adds a repository for the given entity type.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>An instance of <see cref="CachedRepositoryBuilder{T}"/> to build the repository.</returns>
        public CachedRepositoryBuilder<T> AddRepository<T>(Action<CachedRepositoryOptions> configure = null)
        {
            var options = new CachedRepositoryOptions();

            configure?.Invoke(options);

            // Sets the default collection name, if not set
            if (options.CollectionName == null)
            {
                options.CollectionName = typeof(T).Name;
            }

            string collectionName = options.CollectionName;

            // Check for duplicate collection name
            if (_repositoryEntries.Any(e => e.Options.CollectionName.Equals(collectionName)))
            {
                throw new InvalidOperationException("Collection with the given name already registered.");
            }

            // Add repository entry
            _repositoryEntries.Add(new()
            {
                EntityType = typeof(T),
                Options = options,
                RegisterGenericRepository = (services) =>
                {
                    services.AddScoped<ICachedRepository<T>, CachedRepository<T>>(p =>
                    {
                        // Get all repo dependencies for this collection type and name
                        var repositoryDependencies = p.GetServices<IRepositoryDependency<T>>()
                            .Where(dep => dep.TargetCollectionName.Equals(collectionName));

                        // Get all unique constraints for this collection type and name
                        var uniqueConstraints = p.GetServices<IUniqueConstraint<T>>()
                            .Where(c => c.CollectionName.Equals(collectionName));

                        var databaseProvider = p.GetRequiredService<IDatabaseProvider>();

                        CachedRepositoryOptions options = new()
                        {
                            CollectionName = collectionName
                        };

                        return new CachedRepository<T>(
                            databaseProvider,
                            options, 
                            repositoryDependencies: repositoryDependencies, 
                            uniqueConstraints: uniqueConstraints);
                    });
                },
                RepositoryType = typeof(ICachedRepository<T>)
            });

            return new(_builder, _bsonMapper, options);
        }

        /// <summary>
        /// Registers all generic cached repositories that have not been registered.
        /// </summary>
        internal void RegisterGenericRepositories()
        {
            foreach (var entry in _repositoryEntries)
            {
                if (!_builder.Services.Any(c => c.ServiceType == entry.RepositoryType))
                {
                    entry.RegisterGenericRepository(_builder.Services);
                }
            }
        }
    }
}
