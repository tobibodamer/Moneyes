using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace Moneyes.Data
{
    public class KeyCachedRepositoryBuilder<T, TKey>
    {
        private readonly IServiceCollection _services;
        private readonly EntityBuilder<T> _entityBuilder;
        private readonly string _name;
        //private List<Action<EntityBuilder<T>>> _entityBuilderActions = new();

        internal KeyCachedRepositoryBuilder(IServiceCollection serviceCollection,
            EntityBuilder<T> entityBuilder, string collectionName)
        {
            _services = serviceCollection;
            _entityBuilder = entityBuilder;
            _name = collectionName;
        }

        
    }
}
