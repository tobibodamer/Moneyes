using System;
using LiteDB;

namespace Moneyes.Data
{
    internal class BsonMapperFactory
    {
        private readonly Func<BsonMapper> _factoryMethod;
        public BsonMapperFactory(Func<BsonMapper> factoryMethod)
        {
            _factoryMethod = factoryMethod;
        }
        public BsonMapper CreateMapper()
        {
            return _factoryMethod();
        }
    }
}
