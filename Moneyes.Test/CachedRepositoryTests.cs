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
        private readonly ILiteDatabase _database;
        private readonly Random _random = new();
        public CachedRepositoryTests()
        {
            _database = DatabaseHelper.CreateTestDatabase();

            DatabaseHelper.SeedDatabase(_database);
        }

        [Theory]
        [InlineData(NullValueHandling.Ignore)]
        [InlineData(NullValueHandling.Include)]
        public void UniqueConstraintValidationNullValueHandling(NullValueHandling nullValueHandling)
        {
            var uniqueConstraint = DatabaseHelper.MockUniqueConstraint<TestEntity>(
                x => x.CarNumberPlate,
                nameof(TestEntity.CarNumberPlate),
                conflictResolution: ConflictResolution.Fail,
                nullValueHandling: nullValueHandling);

            var repo = DatabaseHelper.SetupRepo<TestEntity, Guid>(_database, x => x.Id,
                new IUniqueConstraint<TestEntity>[] { uniqueConstraint });

            repo.RenewCache();

            var createEntity = () => repo.Create(new TestEntity()
            {
                Id = Guid.NewGuid(),
                Name = Guid.NewGuid().ToString(),
                Age = _random.Next(0, 100),
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

        ~CachedRepositoryTests()
        {
            _database.Dispose();
        }
    }
}