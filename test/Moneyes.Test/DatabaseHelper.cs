using NSubstitute;
using Moneyes.Data;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using LiteDB;
using System.IO;

namespace Moneyes.Test
{
    public static class DatabaseHelper
    {
        public const string TestEntityCollectionName = "TestEntities";
        public static ILiteDatabase CreateTestDatabase()
        {
            return new LiteDatabase(new MemoryStream());
        }

        public static void SeedDatabase(ILiteDatabase database)
        {
            var collection = database.GetCollection<TestEntity>(TestEntityCollectionName);
            collection.Insert(new TestEntity[] {
                new TestEntity()
                {
                    Id = Guid.Parse("ac5aeaa5-5fe6-4550-aff0-c0553482f3a0"),
                    Name = "Peter",
                    Age = 33,
                    CarNumberPlate = "ABXD"
                },
                new TestEntity()
                {
                    Id = Guid.Parse("966a11b5-5483-4b66-8236-ac642dd179a7"),
                    Name = "Ulf",
                    Age = 49,
                    CarNumberPlate = null
                }
            });
        }

        public static ICachedRepository<T, TKey> SetupRepo<T, TKey>(ILiteDatabase database, Func<T, TKey> keySelector, 
            string collectionName,
            IEnumerable<IUniqueConstraint<T>> uniqueConstraints) where TKey : struct
        {
            var databaseProvider = Substitute.For<IDatabaseProvider<LiteDB.ILiteDatabase>>();
            databaseProvider.IsDatabaseCreated.Returns(true);
            databaseProvider.IsOpen.Returns(true);
            databaseProvider.Database.Returns(database);

            return new CachedRepository<T, TKey>(databaseProvider, new() { CollectionName = collectionName },
                Substitute.For<DependencyRefreshHandler>(), keySelector,
                null, uniqueConstraints, Substitute.For<ILogger<CachedRepository<T, TKey>>>());
        }

        public static IUniqueConstraint<T> MockUniqueConstraint<T>(Func<T, object> returnValue, string propertyName = null, string collectionName = null,
            ConflictResolution conflictResolution = default, NullValueHandling nullValueHandling = default)
        {
            var uniqueConstraint = Substitute.For<IUniqueConstraint<T>>();

            uniqueConstraint.GetPropertyValue(Arg.Any<T>()).Returns(x => returnValue(x.Arg<T>()));
            uniqueConstraint.HashPropertyValue(Arg.Any<T>()).Returns(x =>
            {
                var value = returnValue(x.Arg<T>());

                return value?.GetHashCode();
            });
            uniqueConstraint.PropertyName.Returns(propertyName);
            uniqueConstraint.CollectionName.Returns(collectionName ?? TestEntityCollectionName);
            uniqueConstraint.ConflictResolution.Returns(conflictResolution);
            uniqueConstraint.NullValueHandling.Returns(nullValueHandling);

            return uniqueConstraint;
        }
    }
}