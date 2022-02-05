using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace Moneyes.Data
{
    public class CachedRepositoryFactory<T> : IRepositoryFactory<T>
    {
        protected IServiceProvider ServiceProvider { get; }

        public CachedRepositoryFactory(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }
        protected IEnumerable<IUniqueConstraint<T>> GetUniqueConstraints(string collectionName)
        {
            // Get all unique constraints for this collection type and name
            return ServiceProvider.GetServices<IUniqueConstraint<T>>()
                .Where(c => c.CollectionName.Equals(collectionName));
        }

        protected IEnumerable<IRepositoryDependency<T>> GetRepositoryDependencies(string collectionName)
        {
            // Get all repo dependencies for this collection type and name
            return ServiceProvider.GetServices<IRepositoryDependency<T>>()
                .Where(dep => dep.TargetCollectionName.Equals(collectionName));
        }
        public virtual ICachedRepository<T> CreateRepository(CachedRepositoryOptions options, bool autoId)
        {
            string collectionName = options.CollectionName;

            var repositoryDependencies = GetRepositoryDependencies(collectionName);
            var uniqueConstraints = GetUniqueConstraints(collectionName);
            var databaseProvider = ServiceProvider.GetRequiredService<IDatabaseProvider<ILiteDatabase>>();
            var refreshHandler = ServiceProvider.GetRequiredService<DependencyRefreshHandler>();

            // Find a suitable id property or field
            (Type idType, string idName) = FindIdPropertyOfField();

            if (idType is null)
            {
                throw new Exception($"No ID property or field found for type {typeof(T).Name}");
            }

            //// The repository options parameter
            //var optionsParam = Expression.Parameter(typeof(CachedRepositoryOptions));
            //var idPropertyNameParam = Expression.Parameter(typeof(string));
            //var instanceParam = Expression.Parameter(typeof(CachedRepositoryFactory<T>));

            //// Create an expression to call the CreateRepository<TKey> method with the ID property type
            //var createRepoMethodCall = Expression.Call(instanceParam, nameof(CreateRepositoryDynamic),
            //    new Type[] { idType }, optionsParam, idPropertyNameParam, instanceParam);

            //// Create a factory lambda expression to create a repository with the id type
            //var factory = Expression.Lambda<Func<CachedRepositoryOptions, string, CachedRepositoryFactory<T>, ICachedRepository<T>>>(createRepoMethodCall)
            //    .Compile();


            var thisType = typeof(CachedRepositoryFactory<T>);
            var createMethod = thisType.GetMethod(nameof(CreateRepositoryDynamic), BindingFlags.NonPublic | BindingFlags.Instance);
            var genericCreateMethod = createMethod.MakeGenericMethod(idType);

            return (ICachedRepository<T>)
                genericCreateMethod.Invoke(this, new object[] { options, idName });
            
            // Create the repository using the factory method.
            //return factory(options, idName, this);
        }

        private static (Type type, string name) FindIdPropertyOfField()
        {
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
            var entityType = typeof(T);

            var properties = entityType.GetFields(bindingFlags).Select(x => (x.FieldType, x.Name));
            var fields = entityType.GetProperties(bindingFlags).Select(x => (x.PropertyType, x.Name));
            
            return properties.Concat(fields)
                .FirstOrDefault(x => x.Name.ToLower().Equals("id"));
        }

        /// <summary>
        /// Creates an ID selector expression for an ID field with the given name.
        /// </summary>
        /// <typeparam name="TKey">The ID field type.</typeparam>
        /// <param name="idPropertyName">The ID field name.</param>
        /// <returns></returns>
        private static Expression<Func<T, TKey>> CreateIdSelector<TKey>(string idPropertyName)
        {
            var parameter = Expression.Parameter(typeof(T));
            var idProperty = Expression.PropertyOrField(parameter, idPropertyName);
            var idSelector = Expression.Lambda<Func<T, TKey>>(idProperty, parameter);

            return idSelector;
        }

        /// <summary>
        /// This method is used to create a repostory with key type derived at runtime.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="options"></param>
        /// <param name="idPropertyName">The ID field name.</param>
        /// <returns></returns>
        private ICachedRepository<T, TKey> CreateRepositoryDynamic<TKey>(CachedRepositoryOptions options, string idPropertyName)
            where TKey : struct
        {
            string collectionName = options.CollectionName;

            var repositoryDependencies = GetRepositoryDependencies(collectionName);
            var uniqueConstraints = GetUniqueConstraints(collectionName);
            var databaseProvider = ServiceProvider.GetRequiredService<IDatabaseProvider<ILiteDatabase>>();
            var dependencyRefreshHandler = ServiceProvider.GetService<DependencyRefreshHandler>();

            var idSelector = CreateIdSelector<TKey>(idPropertyName).Compile();

            return new CachedRepository<T, TKey>(
                databaseProvider,
                options,
                dependencyRefreshHandler,
                idSelector,
                repositoryDependencies,
                uniqueConstraints);
        }
    }

    public class CachedRepositoryFactory<T, TKey> : CachedRepositoryFactory<T>, IRepositoryFactory<T, TKey>
        where TKey : struct
    {
        public CachedRepositoryFactory(IServiceProvider serviceProvider)
            : base(serviceProvider) { }

        public virtual ICachedRepository<T, TKey> CreateRepository(CachedRepositoryOptions options, Func<T, TKey> keySelector, bool autoId)
        {
            string collectionName = options.CollectionName;

            var repositoryDependencies = GetRepositoryDependencies(collectionName);
            var uniqueConstraints = GetUniqueConstraints(collectionName);
            var databaseProvider = ServiceProvider.GetRequiredService<IDatabaseProvider<ILiteDatabase>>();
            var dependencyRefreshHandler = ServiceProvider.GetService<DependencyRefreshHandler>();

            return new CachedRepository<T, TKey>(
                databaseProvider,
                options,
                dependencyRefreshHandler,
                keySelector,
                repositoryDependencies,
                uniqueConstraints);
        }
    }
}
