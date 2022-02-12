using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Moneyes.Data
{
    public class CachedRepositoriesBuilder
    {
        private readonly LiteDbBuilder _builder;
        private readonly BsonMapper _bsonMapper = new();
        private readonly List<RepositoryEntry> _repositoryEntries = new();
        internal CachedRepositoriesBuilder(LiteDbBuilder liteDbBuilder)
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
            public Action<IServiceCollection> RegisterDefaultFactory { get; init; }
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
            return AddRepository<T, ICachedRepository<T>>(collectionName);
        }

        /// <summary>
        /// Adds a repository for the given entity type.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>An instance of <see cref="CachedRepositoryBuilder{T}"/> to build the repository.</returns>
        public CachedRepositoryBuilder<T> AddRepository<T>(Action<CachedRepositoryOptions> configure = null)
        {
            return AddRepository<T, ICachedRepository<T>>(configure);
        }


        /// <summary>
        /// Adds a custom cached repository for the given entity type.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="collectionName">Custom table name.</param>
        /// <returns>An instance of <see cref="CachedRepositoryBuilder{T}"/> to build the repository.</returns>
        public CachedRepositoryBuilder<T> AddRepository<T, TRepository>(string collectionName)
            where TRepository : class, ICachedRepository<T>
        {
            return AddRepository<T, TRepository>(options => options.CollectionName = collectionName);
        }

        /// <summary>
        /// Adds a custom cached repository for the given entity type.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>An instance of <see cref="CachedRepositoryBuilder{T}"/> to build the repository.</returns>
        public CachedRepositoryBuilder<T> AddRepository<T, TRepository>(Action<CachedRepositoryOptions> configure = null)
            where TRepository : class, ICachedRepository<T>
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

            CachedRepositoryBuilder<T> repoBuilder = new(_builder, _bsonMapper.Entity<T>(), options);

            // Add repository entry
            _repositoryEntries.Add(new()
            {
                EntityType = typeof(T),
                Options = options,
                RegisterDefaultFactory = (services) =>
                {
                    // Add default repository factory if no factory registered

                    if (!services.Any(c => typeof(IRepositoryFactory<T>).IsAssignableFrom(c.ServiceType)))
                    {
                        services.AddScoped<IRepositoryFactory<T>, CachedRepositoryFactory<T>>();
                    }
                },
                RegisterGenericRepository = (services) =>
                {
                    services.AddScoped<ICachedRepository<T>>(p =>
                    {
                        IRepositoryFactory<T> repositoryFactory;

                        if (repoBuilder.FactoryType != null && typeof(IRepositoryFactory<T>).IsAssignableFrom(repoBuilder.FactoryType))
                        {
                            // Use custom repo factory is set and valid
                            repositoryFactory = ActivatorUtilities.CreateInstance(p, repoBuilder.FactoryType) as IRepositoryFactory<T>;
                        }
                        else
                        {
                            // Use default registration otherwise
                            repositoryFactory = p.GetRequiredService<IRepositoryFactory<T>>();
                        }

                        return repositoryFactory.CreateRepository(options, autoId: false);

                    });
                },
                RepositoryType = typeof(ICachedRepository<T>)
            });

            return repoBuilder;
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

                entry.RegisterDefaultFactory(_builder.Services);

            }
        }

        internal void RegisterRepositoryProvider()
        {
            if (!_builder.Services.Any(c => c.ServiceType == typeof(IRepositoryProvider)))
            {
                _builder.Services.AddScoped<IRepositoryProvider, RepositoryProvider>(p => new(p));
            }
        }
    }
}
