using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace Moneyes.Data
{
    public class CachedRepositoryBuilder<T>
    {
        private readonly IServiceCollection _services;
        private readonly string _name;
        private readonly EntityBuilder<T> _entityBuilder;

        internal CachedRepositoryBuilder(LiteDbBuilder builder, BsonMapper bsonMapper,
            CachedRepositoryOptions options)
        {
            _services = builder.Services;
            _name = options.CollectionName;
            _entityBuilder = bsonMapper.Entity<T>();
        }

        /// <summary>
        /// Registers a primary key for this repository and the underlying collection.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="keySelector"></param>
        /// <param name="autoId"></param>
        /// <returns></returns>
        public CachedRepositoryBuilder<T> HasKey<TKey>(Expression<Func<T, TKey>> keySelector, bool autoId = false)
            where TKey : struct
        {
            // Register the repository with strongly typed primary key

            _services.AddScoped<ICachedRepository<T, TKey>, CachedRepository<T, TKey>>(p =>
            {
                // Get all repo dependencies for this collection type and name
                var repositoryDependencies = p.GetServices<IRepositoryDependency<T>>()
                    .Where(dep => dep.TargetCollectionName.Equals(_name));

                // Get all unique constraints for this collection type and name
                var uniqueConstraints = p.GetServices<IUniqueConstraint<T>>()
                    .Where(c => c.CollectionName.Equals(_name));

                var databaseProvider = p.GetRequiredService<IDatabaseProvider>();

                CachedRepositoryOptions options = new()
                {
                    CollectionName = _name
                };

                return new(databaseProvider, keySelector.Compile(), options, autoId, repositoryDependencies, uniqueConstraints);
            });

            // Register generic repository without primary key type
            _services.AddScoped<ICachedRepository<T>>(p =>
            {
                var repository = p.GetServices<ICachedRepository<T, TKey>>()
                                    .First(r => r.CollectionName.Equals(_name));

                return repository;
            });

            // Configure primary key in entity builder
            _entityBuilder.Id(keySelector, autoId);

            return this;
        }

        /// <summary>
        /// Registers a reference from a entity property to entities in another repository, 
        /// also in the underlying collection.
        /// </summary>
        /// <typeparam name="TDep"></typeparam>
        /// <param name="propertySelector"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public CachedRepositoryBuilder<T> DependsOnOne<TDep>(Expression<Func<T, TDep>> propertySelector, string collection = null)
        {
            _services.AddScoped<IRepositoryDependency<T>, RepositoryDependency<T, TDep>>(p =>
            {
                var repositoryProvider = p.GetRequiredService<IRepositoryProvider>();

                return new(repositoryProvider, propertySelector,
                    targetCollection: _name, sourceCollection: collection);
            });
            
            // Configure reference in entity builder
            _entityBuilder.DbRef(propertySelector, collection);

            return this;
        }

        /// <summary>
        /// Registers a reference from a entity collection property to entities in another repository, 
        /// also in the underlying collection.
        /// </summary>
        /// <typeparam name="TDep"></typeparam>
        /// <param name="collectionPropertySelector"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public CachedRepositoryBuilder<T> DependsOnMany<TDep>(Expression<Func<T, IEnumerable<TDep>>> collectionPropertySelector,
            string collection = null)
        {
            _services.AddScoped<IRepositoryDependency<T>, RepositoryDependency<T, TDep>>(p =>
            {
                var repositoryProvider = p.GetRequiredService<IRepositoryProvider>();

                return new(repositoryProvider, collectionPropertySelector,
                    targetCollection: _name, sourceCollection: collection);
            });

            // Configure reference in entity builder
            _entityBuilder.DbRef(collectionPropertySelector, collection);

            return this;
        }

        /// <summary>
        /// Adds a unique constraint to a entity property in this repository.
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="selector"></param>
        /// <returns></returns>
        public CachedRepositoryBuilder<T> WithUniqueProperty<K>(Expression<Func<T, K>> selector)
        {
            _services.AddScoped<IUniqueConstraint<T>, UniqueConstraint<T, K>>(p =>
            {
                return new(selector, _name);
            });

            return this;
        }
    }
}
