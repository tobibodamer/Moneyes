using Xunit;
using Moneyes.Data;
using FluentAssertions.Extensions;
using System;
using System.Linq.Expressions;
using LiteDB;
using System.IO;
using FluentAssertions;
using NSubstitute.ExceptionExtensions;

namespace Moneyes.Test
{
    public class CachedRepositoryTests
    {
        [Theory]
        [InlineData(NullValueHandling.Ignore)]
        [InlineData(NullValueHandling.Include)]
        public void UniqueConstraintValidationNullValueHandling(NullValueHandling nullValueHandling)
        {
            using var database = DatabaseHelper.CreateTestDatabase();

            const string collectionName = "TestEntity";

            var collection = database.GetCollection<TestEntity>(collectionName);
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

            var uniqueConstraint = DatabaseHelper.MockUniqueConstraint<TestEntity>(
                x => x.CarNumberPlate,
                nameof(TestEntity.CarNumberPlate),
                collectionName: collectionName,
                conflictResolution: ConflictResolution.Fail,
                nullValueHandling: nullValueHandling);

            var repo = DatabaseHelper.SetupRepo<TestEntity, Guid>(database, x => x.Id, collectionName,
                new IUniqueConstraint<TestEntity>[] { uniqueConstraint });

            repo.RenewCache();

            var createEntity = () => repo.Create(new TestEntity()
            {
                Id = Guid.NewGuid(),
                Name = "wertzujikl",
                Age = 77,
                CarNumberPlate = null
            });

            if (nullValueHandling is NullValueHandling.Ignore)
            {
                createEntity().Should().NotBeNull();
            }
            else if (nullValueHandling is NullValueHandling.Include)
            {
                createEntity.Should().Throw<ConstraintViolationException>()
                    .Where(e => e.PropertyName.Equals(nameof(TestEntity.CarNumberPlate)));
            }
        }

        [Theory]
        [InlineData(NullValueHandling.Ignore)]
        [InlineData(NullValueHandling.Include)]
        public void UniqueConstraintValidationExistingViolationShouldNotCauseError(NullValueHandling nullValueHandling)
        {
            using var database = DatabaseHelper.CreateTestDatabase();

            const string collectionName = "TestEntity";

            var collection = database.GetCollection<TestEntity>(collectionName);
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
                    Id = Guid.Parse("ffb8a4bd-d24b-4ccc-8a22-8b02632a30e2"),
                    Name = "Manuel",
                    Age = 34,
                    CarNumberPlate = "ABXD"
                },
                new TestEntity()
                {
                    Id = Guid.Parse("966a11b5-5483-4b66-8236-ac642dd179a7"),
                    Name = "Ulf",
                    Age = 49,
                    CarNumberPlate = null
                },
                new TestEntity()
                {
                    Id = Guid.Parse("2b086903-205c-4025-b3e6-8c3cdeb8ae61"),
                    Name = "Gudrun",
                    Age = 52,
                    CarNumberPlate = null
                }
            });

            var uniqueConstraint = DatabaseHelper.MockUniqueConstraint<TestEntity>(
                x => x.CarNumberPlate,
                nameof(TestEntity.CarNumberPlate),
                collectionName: collectionName,
                conflictResolution: ConflictResolution.Fail,
                nullValueHandling: nullValueHandling);

            var repo = DatabaseHelper.SetupRepo<TestEntity, Guid>(database, x => x.Id, collectionName,
                new IUniqueConstraint<TestEntity>[] { uniqueConstraint });

            repo.RenewCache();

            var createEntity = () => repo.Create(new TestEntity()
            {
                Id = Guid.NewGuid(),
                Name = "wertzujikl",
                Age = 77,
                CarNumberPlate = null
            });

            if (nullValueHandling is NullValueHandling.Ignore)
            {
                createEntity().Should().NotBeNull();
            }
            else if (nullValueHandling is NullValueHandling.Include)
            {
                createEntity.Should().Throw<ConstraintViolationException>()
                    .Where(e => e.PropertyName.Equals(nameof(TestEntity.CarNumberPlate)));
            }
        }
    }
}