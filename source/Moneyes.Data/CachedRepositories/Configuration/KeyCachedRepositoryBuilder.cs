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
        protected IServiceCollection Services { get; }
        protected string Name { get; }
        protected EntityBuilder<T> EntityBuilder { get; set; }
        protected CachedRepositoryOptions Options { get; }
        protected LiteDbBuilder DbBuilder { get; }

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
             where TDep : class
        {
            collection ??= typeof(TDep).Name;

            Services.AddScoped<IRepositoryDependency<T>, RepositoryDependency<T, TDep>>(p =>
            {
                var repositoryProvider = p.GetRequiredService<IRepositoryProvider>();
                var dependencyRefreshHandler = p.GetRequiredService<DependencyRefreshHandler>();

                return new RepositoryDependency<T, TDep>(
                    repositoryProvider, propertySelector,
                    targetCollection: Name, sourceCollection: collection);
            });

            // Add DependencyRefreshHandler if not yet registered
            if (!Services.Any(x => x.ServiceType == typeof(DependencyRefreshHandler)))
            {
                Services.AddScoped<DependencyRefreshHandler>();
            }

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
        public virtual KeyCachedRepositoryBuilder<T> DependsOnMany<TDep>(Expression<Func<T, ICollection<TDep>>> collectionPropertySelector,
            string collection = null) where TDep : class
        {
            collection ??= typeof(TDep).Name;

            Services.AddScoped<IRepositoryDependency<T>, RepositoryDependency<T, TDep>>(p =>
            {
                var repositoryProvider = p.GetRequiredService<IRepositoryProvider>();
                var dependencyRefreshHandler = p.GetRequiredService<DependencyRefreshHandler>();

                return new RepositoryDependency<T, TDep>(
                    repositoryProvider, collectionPropertySelector,
                    targetCollection: Name, sourceCollection: collection);
            });

            // Add DependencyRefreshHandler if not yet registered
            if (!Services.Any(x => x.ServiceType == typeof(DependencyRefreshHandler)))
            {
                Services.AddScoped<DependencyRefreshHandler>();
            }

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

        /// <summary>
        /// Specifies a factory to use for creating cached repository instances.
        /// </summary>
        /// <typeparam name="TFactory">The factory type.</typeparam>
        /// <returns></returns>
        public virtual KeyCachedRepositoryBuilder<T> UseFactory<TFactory>() where TFactory : IRepositoryFactory<T>
        {
            FactoryType = typeof(TFactory);

            return this;
        }
    }
}
