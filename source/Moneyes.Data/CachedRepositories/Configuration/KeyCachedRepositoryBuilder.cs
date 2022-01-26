using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace Moneyes.Data
{
    public class KeyCachedRepositoryBuilder<T>
    {
        protected readonly CachedRepositoryBuilder<T> _baseBuilder;
        protected readonly IServiceCollection Services;
        protected readonly string Name;
        protected readonly EntityBuilder<T> EntityBuilder;
        protected readonly CachedRepositoryOptions Options;
        protected readonly LiteDbBuilder DbBuilder;

#nullable enable
        internal Type? FactoryType { get; set; }
        protected Type? RepositoryType { get; set; }
#nullable disable

        internal KeyCachedRepositoryBuilder(LiteDbBuilder builder, EntityBuilder<T> entityBuilder,
            CachedRepositoryOptions options)
        {
            Services = builder.Services;
            Name = options.CollectionName;
            Options = options;
            EntityBuilder = entityBuilder;
            DbBuilder = builder;
        }

        /// <summary>
        /// Registers a reference from a entity property to entities in another repository, 
        /// also in the underlying collection.
        /// </summary>
        /// <typeparam name="TDep"></typeparam>
        /// <param name="propertySelector"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public virtual KeyCachedRepositoryBuilder<T> DependsOnOne<TDep>(Expression<Func<T, TDep>> propertySelector, string collection = null)
        {
            Services.AddScoped<IRepositoryDependency<T>, RepositoryDependency<T, TDep>>(p =>
            {
                var repositoryProvider = p.GetRequiredService<IRepositoryProvider>();

                return new(repositoryProvider, propertySelector,
                    targetCollection: Name, sourceCollection: collection);
            });

            // Configure reference in entity builder
            EntityBuilder.DbRef(propertySelector, collection);

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
        public virtual KeyCachedRepositoryBuilder<T> DependsOnMany<TDep>(Expression<Func<T, IEnumerable<TDep>>> collectionPropertySelector,
            string collection = null)
        {
            Services.AddScoped<IRepositoryDependency<T>, RepositoryDependency<T, TDep>>(p =>
            {
                var repositoryProvider = p.GetRequiredService<IRepositoryProvider>();

                return new(repositoryProvider, collectionPropertySelector,
                    targetCollection: Name, sourceCollection: collection);
            });

            // Configure reference in entity builder
            EntityBuilder.DbRef(collectionPropertySelector, collection);

            return this;
        }

        /// <summary>
        /// Adds a unique constraint to a entity property in this repository.
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="selector"></param>
        /// <returns></returns>
        public virtual KeyCachedRepositoryBuilder<T> WithUniqueProperty<K>(Expression<Func<T, K>> selector)
        {
            Services.AddScoped<IUniqueConstraint<T>, UniqueConstraint<T, K>>(p =>
            {
                return new(selector, Name);
            });

            return this;
        }

        /// <summary>
        /// Registers a specific repository type, that is produced by the <see cref="ICachedRepositoryFactory"/>.
        /// </summary>
        /// <typeparam name="TOther"></typeparam>
        /// <returns></returns>
        public virtual KeyCachedRepositoryBuilder<T> As<TOther>() where TOther : class, ICachedRepository<T>
        {
            RepositoryType = typeof(TOther);

            if (typeof(ICachedRepository<T>).IsAssignableFrom(RepositoryType))
            {
                // Reigster specific repository type
                Services.AddScoped(RepositoryType, p =>
                {
                    var repository = p.GetServices<ICachedRepository<T>>()
                                        .First(r => r.CollectionName.Equals(Name));

                    return repository;
                });
            }

            return this;
        }

        public virtual KeyCachedRepositoryBuilder<T> UseFactory<TFactory>() where TFactory : IRepositoryFactory<T>
        {
            FactoryType = typeof(TFactory);

            return this;
        }
    }
}
