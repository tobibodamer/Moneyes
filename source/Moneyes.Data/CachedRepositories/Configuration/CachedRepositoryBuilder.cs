using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace Moneyes.Data
{
    internal abstract class CachedRepositoryBuilderBase<T, TRepository, TBuilder> 
        where TRepository : class, ICachedRepository<T>
        where TBuilder : CachedRepositoryBuilderBase<T, TRepository, TBuilder>
    {
        private readonly IServiceCollection _services;
        private readonly string _name;
        private readonly EntityBuilder<T> _entityBuilder;
        private readonly CachedRepositoryOptions _options;
        private readonly LiteDbBuilder _builder;

#nullable enable
        internal Type? FactoryType { get; set; }
#nullable disable

        internal CachedRepositoryBuilderBase(LiteDbBuilder builder, EntityBuilder<T> entityBuilder,
            CachedRepositoryOptions options, bool isKeySet = false)
        {
            _services = builder.Services;
            _name = options.CollectionName;
            _options = options;
            _entityBuilder = entityBuilder;
            _builder = builder;
        }

        /// <summary>
        /// Registers a reference from a entity property to entities in another repository, 
        /// also in the underlying collection.
        /// </summary>
        /// <typeparam name="TDep"></typeparam>
        /// <param name="propertySelector"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public TBuilder DependsOnOne<TDep>(Expression<Func<T, TDep>> propertySelector, string collection = null)
        {
            _services.AddScoped<IRepositoryDependency<T>, RepositoryDependency<T, TDep>>(p =>
            {
                var repositoryProvider = p.GetRequiredService<IRepositoryProvider>();

                return new(repositoryProvider, propertySelector,
                    targetCollection: _name, sourceCollection: collection);
            });

            // Configure reference in entity builder
            _entityBuilder.DbRef(propertySelector, collection);

            return this as TBuilder;
        }

        /// <summary>
        /// Registers a reference from a entity collection property to entities in another repository, 
        /// also in the underlying collection.
        /// </summary>
        /// <typeparam name="TDep"></typeparam>
        /// <param name="collectionPropertySelector"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public TBuilder DependsOnMany<TDep>(Expression<Func<T, IEnumerable<TDep>>> collectionPropertySelector,
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

            return this as TBuilder;
        }

        /// <summary>
        /// Adds a unique constraint to a entity property in this repository.
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="selector"></param>
        /// <returns></returns>
        public TBuilder WithUniqueProperty<K>(Expression<Func<T, K>> selector)
        {
            _services.AddScoped<IUniqueConstraint<T>, UniqueConstraint<T, K>>(p =>
            {
                return new(selector, _name);
            });

            return this as TBuilder;
        }

        ///// <summary>
        ///// Registers a specific repository typed, that is produced by the <see cref="ICachedRepositoryFactory"/>.
        ///// </summary>
        ///// <typeparam name="TOther"></typeparam>
        ///// <returns></returns>
        //public CachedRepositoryBuilder<T, TOther> As<TOther>() where TOther : class, TRepository
        //{
        //    return new CachedRepositoryBuilder<T, TOther>(_builder, _entityBuilder, _options, _isKeySet);
        //}

        public TBuilder UseFactory<TFactory>() where TFactory : IRepositoryFactory<T>
        {
            FactoryType = typeof(TFactory);
            return this as TBuilder;
        }
    }
    public class CachedRepositoryBuilder<T> : KeyCachedRepositoryBuilder<T>
    {
        internal CachedRepositoryBuilder(
            LiteDbBuilder builder, 
            EntityBuilder<T> entityBuilder, 
            CachedRepositoryOptions options)
            : base(builder, entityBuilder, options)
        {
        }


        /// <summary>
        /// Registers a primary key for this repository and the underlying collection.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="keySelector"></param>
        /// <param name="autoId"></param>
        /// <returns></returns>
        public virtual KeyCachedRepositoryBuilder<T> HasKey<TKey>(Expression<Func<T, TKey>> keySelector, bool autoId = false)
            where TKey : struct
        {
            // Add default repository factory if no factory registered

            if (!Services.Any(c => typeof(IRepositoryFactory<T, TKey>).IsAssignableFrom(c.ServiceType)))
            {
                Services.AddScoped<IRepositoryFactory<T, TKey>, CachedRepositoryFactory<T, TKey>>();
            }

            // Register the repository with strongly typed primary key
            Services.AddScoped<ICachedRepository<T, TKey>>(p =>
            {
                IRepositoryFactory<T, TKey> repositoryFactory;

                if (FactoryType != null && typeof(IRepositoryFactory<T, TKey>).IsAssignableFrom(FactoryType))
                {
                    repositoryFactory = ActivatorUtilities.CreateInstance(p, FactoryType) as IRepositoryFactory<T, TKey>;
                }
                else
                {
                    repositoryFactory = p.GetRequiredService<IRepositoryFactory<T, TKey>>();
                }

                CachedRepositoryOptions options = new()
                {
                    CollectionName = Name
                };

                return repositoryFactory.CreateRepository(options, keySelector.Compile(), autoId);
            });

            // Register generic repository without primary key type
            Services.AddScoped<ICachedRepository<T>>(p =>
            {
                var repository = p.GetServices<ICachedRepository<T, TKey>>()
                                    .First(r => r.CollectionName.Equals(Name));

                return repository;
            });

            //if (RepositoryType != null && typeof(ICachedRepository<T, TKey>).IsAssignableFrom(RepositoryType))
            //{
            //    // Reigster specific repository type
            //    Services.AddScoped(RepositoryType, p =>
            //    {
            //        var repository = p.GetServices<ICachedRepository<T>>()
            //                            .First(r => r.CollectionName.Equals(Name));

            //        return repository;
            //    });
            //}

            // Configure primary key in entity builder
            EntityBuilder.Id(keySelector, autoId);

            return this;
        }

        public override CachedRepositoryBuilder<T> DependsOnOne<TDep>(Expression<Func<T, TDep>> propertySelector, string collection = null)
        {
            return base.DependsOnOne(propertySelector, collection) as CachedRepositoryBuilder<T>;
        }

        public override CachedRepositoryBuilder<T> DependsOnMany<TDep>(Expression<Func<T, IEnumerable<TDep>>> collectionPropertySelector, string collection = null)
        {
            return base.DependsOnMany(collectionPropertySelector, collection) as CachedRepositoryBuilder<T>;
        }

        public override CachedRepositoryBuilder<T> WithUniqueProperty<K>(Expression<Func<T, K>> selector)
        {
            return base.WithUniqueProperty(selector) as CachedRepositoryBuilder<T>;
        }

        public override CachedRepositoryBuilder<T> UseFactory<TFactory>()
        {
            return base.UseFactory<TFactory>() as CachedRepositoryBuilder<T>;
        }

        public override CachedRepositoryBuilder<T> As<TOther>()
        {
            return base.As<TOther>() as CachedRepositoryBuilder<T>;
        }
    }
}
