using System;
using Microsoft.Extensions.DependencyInjection;

namespace Moneyes.Data
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds LiteDB services with the default <see cref="DatabaseProvider"/>.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="databasePath">The path to the database file.</param>
        /// <returns></returns>
        public static LiteDbBuilder AddLiteDb(this IServiceCollection services, string databasePath)
        {
            return AddLiteDb(services, config => config.DatabasePath = databasePath);
        }

        /// <summary>
        /// Adds LiteDB services with the default <see cref="DatabaseProvider"/>.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static LiteDbBuilder AddLiteDb(this IServiceCollection services, Action<LiteDbConfig> configure)
        {
            return AddLiteDb<DatabaseProvider>(services, configure);
        }

        /// <summary>
        /// Adds LiteDB services with a custom <see cref="DatabaseProvider"/>.
        /// </summary>
        /// <typeparam name="TProvider"></typeparam>
        /// <param name="services"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static LiteDbBuilder AddLiteDb<TProvider>(this IServiceCollection services, Action<LiteDbConfig> configure)
            where TProvider : DatabaseProvider
        {
            // Add database provider
            services.AddScoped<IDatabaseProvider, TProvider>();

            LiteDbBuilder builder = new(services);

            // Add config
            services.AddTransient<LiteDbConfig>(p =>
            {
                LiteDbConfig config = new();

                // Create BsonMapper from factory if existing
                var bsonMapperFactory = p.GetService<BsonMapperFactory>();

                if (bsonMapperFactory != null)
                {
                    config.BsonMapper = bsonMapperFactory.CreateMapper();
                }

                configure?.Invoke(config);

                return config;
            });

            services.AddScoped<LiteDbFactory>();

            return builder;
        }

        /// <summary>
        /// Adds cached repositories to the LiteDB setup.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static LiteDbBuilder AddCachedRepositories(this LiteDbBuilder builder, Action<CachedRepositoriesOptions> configureOptions)
        {
            CachedRepositoriesOptions options = new(builder);

            configureOptions?.Invoke(options);

            options.RegisterGenericRepositories();
            options.RegisterRepositoryProvider();

            return builder;
        }
    }
}
