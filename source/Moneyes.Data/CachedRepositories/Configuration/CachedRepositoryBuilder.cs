using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace Moneyes.Data
{
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

                return repositoryFactory.CreateRepository(Options, keySelector.Compile(), autoId);
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

        public override CachedRepositoryBuilder<T> DependsOnMany<TDep>(Expression<Func<T, ICollection<TDep>>> collectionPropertySelector, string collection = null)
        {
            return base.DependsOnMany(collectionPropertySelector, collection) as CachedRepositoryBuilder<T>;
        }

        public override CachedRepositoryBuilder<T> WithUniqueProperty<K>(Expression<Func<T, K>> selector, ConflictResolution conflictResolution = default)
        {
            return base.WithUniqueProperty(selector, conflictResolution) as CachedRepositoryBuilder<T>;
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
