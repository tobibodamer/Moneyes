using Xunit;
using Moneyes.Data;
using FluentAssertions.Extensions;
using System;
using System.IO;
using FluentAssertions;
using NSubstitute.ExceptionExtensions;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;


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
                Name = Guid.NewGuid().ToString(),
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
        [InlineData(ConflictResolution.Fail)]
        [InlineData(ConflictResolution.Ignore)]
        [InlineData(ConflictResolution.Replace)]
        public void ConflictResolution_update_has_lower_weight_than_all_other(ConflictResolution higher)
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
                }
            });

            var uniqueConstraintHigher = DatabaseHelper.MockUniqueConstraint<TestEntity>(
                x => x.CarNumberPlate,
                nameof(TestEntity.CarNumberPlate),
                collectionName: collectionName,
                conflictResolution: higher,
                nullValueHandling: default);

            var uniqueConstraintLower = DatabaseHelper.MockUniqueConstraint<TestEntity>(
                x => x.Name,
                nameof(TestEntity.Name),
                collectionName: collectionName,
                conflictResolution: default,
                nullValueHandling: default);


            var repo = DatabaseHelper.SetupRepo<TestEntity, Guid>(database, x => x.Id, collectionName,
                new IUniqueConstraint<TestEntity>[] { uniqueConstraintHigher, uniqueConstraintLower });

            repo.RenewCache();

            var conflictHandler = Substitute.For<ConflictResolutionDelegate<TestEntity>>();

            conflictHandler.Invoke(Arg.Any<ConflictResolutionFactory<TestEntity>>()).Returns(x =>
            {
                var factory = x.Arg<ConflictResolutionFactory<TestEntity>>();

                return factory.Dynamic(v =>
                {
                    if (v.Constraint == uniqueConstraintHigher)
                    {
                        return new ConflictResolutionAction(higher);
                    }

                    return factory.Update((old, @new) =>
                        new()
                        {
                            Id = old.Id,
                            Age = 99,
                            CarNumberPlate = @new.CarNumberPlate,
                            Name = @new.Name
                        });
                });
            });

            var newEntity = new TestEntity()
            {
                Id = Guid.NewGuid(),
                Name = "Peter",
                Age = 11,
                CarNumberPlate = "ABXD"
            };

            var createEntity = () => repo.CreateMany(new[] {
              newEntity
            }, conflictHandler);

            switch (higher)
            {
                case ConflictResolution.Fail:
                    createEntity.Should().Throw<ConstraintViolationException>()
                    .Where(e => e.PropertyName.Equals(nameof(TestEntity.CarNumberPlate)));
                    break;
                case ConflictResolution.Ignore:
                    createEntity().Should().Be(0);
                    collection.FindAll().Should().ContainSingle();
                    collection.FindAll().Should().Contain(x => x.Id == new Guid("ac5aeaa5-5fe6-4550-aff0-c0553482f3a0"));
                    break;
                case ConflictResolution.Replace:
                    createEntity().Should().Be(1);
                    collection.FindAll().Should().ContainSingle();
                    collection.FindAll().Should().Contain(newEntity);
                    break;
            }

            // Make sure update didnt get through
            collection.FindAll().Should().NotContain(x => x.Age == 99);
        }

        //[Theory]
        //[InlineData(ConflictResolution.Fail)]
        //[InlineData(ConflictResolution.Ignore)]
        //[InlineData(ConflictResolution.Replace)]
        //public void ConflictResolution_Update_affect_validation_of_following_entities(ConflictResolution higher)
        //{
        //    using var database = DatabaseHelper.CreateTestDatabase();
        //    database.Mapper.Entity<TestEntity>().Id(x => x.Id, false);

        //    const string collectionName = "TestEntity";

        //    var collection = database.GetCollection<TestEntity>(collectionName);
        //    collection.Insert(new TestEntity[] {
        //        new TestEntity()
        //        {
        //            Id = Guid.Parse("ac5aeaa5-5fe6-4550-aff0-c0553482f3a0"),
        //            Name = "Peter",
        //            Age = 33,
        //            CarNumberPlate = "ABXD"
        //        }
        //    });

        //    var uniqueConstraint = DatabaseHelper.MockUniqueConstraint<TestEntity>(
        //        x => x.CarNumberPlate,
        //        nameof(TestEntity.CarNumberPlate),
        //        collectionName: collectionName,
        //        conflictResolution: higher,
        //        nullValueHandling: default);


        //    var repo = DatabaseHelper.SetupRepo<TestEntity, Guid>(database, x => x.Id, collectionName,
        //        new IUniqueConstraint<TestEntity>[] { uniqueConstraint });

        //    repo.RenewCache();

        //    var conflictHandler = Substitute.For<ConflictResolutionDelegate<TestEntity>>();

        //    conflictHandler.Invoke(Arg.Any<ConstraintViolation<TestEntity>>()).Returns(x =>
        //    {
        //        var v = x.Arg<ConstraintViolation<TestEntity>>();

        //        if (v.NewEntity.Name.Equals("Peter"))
        //        {

        //            return ConflictResolutionAction.Update(new TestEntity()
        //            {
        //                Id = v.ExistingEntity.Id,
        //                Age = 99,
        //                CarNumberPlate = "1234",
        //                Name = v.NewEntity.Name
        //            });
        //        }
        //        else
        //        {
        //            return new ConflictResolutionAction(higher);
        //        }
        //    });


        //    var createEntity = () => repo.CreateMany(new[] {
        //        new TestEntity()
        //        {
        //            Id = Guid.NewGuid(),
        //            Name = "Peter",
        //            Age = 11,
        //            CarNumberPlate = "ABXD"
        //        },
        //        new TestEntity()
        //        {
        //            Id = Guid.NewGuid(),
        //            Name = "Murat",
        //            Age = 19,
        //            CarNumberPlate = "1234"
        //        }
        //    }, conflictHandler);

        //    switch (higher)
        //    {
        //        case ConflictResolution.Fail:
        //            createEntity.Should().Throw<ConstraintViolationException>()
        //                .Where(e => e.PropertyName.Equals(nameof(TestEntity.CarNumberPlate)));
        //            break;
        //        case ConflictResolution.Ignore:
        //            createEntity().Should().Be(0);
        //            break;
        //        case ConflictResolution.Replace:
        //            createEntity().Should().Be(1);
        //            collection.FindAll().Should().ContainSingle();
        //            collection.FindAll().Should().Contain(x => x.Name.Equals("Murat"));
        //            return;
        //    }

        //    collection.FindAll().Should().ContainSingle();
        //    collection.FindAll().Should().Contain(x => x.Id == new Guid("ac5aeaa5-5fe6-4550-aff0-c0553482f3a0"));
        //}

        [Theory]
        [InlineData(ConflictResolution.Fail, ConflictResolution.Ignore)]
        [InlineData(ConflictResolution.Ignore, ConflictResolution.Replace)]
        public void ConflictResolution_Weight(ConflictResolution higher, ConflictResolution lower)
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
                }
            });

            var uniqueConstraintHigher = DatabaseHelper.MockUniqueConstraint<TestEntity>(
                x => x.CarNumberPlate,
                nameof(TestEntity.CarNumberPlate),
                collectionName: collectionName,
                conflictResolution: higher,
                nullValueHandling: default);

            var uniqueConstraintLower = DatabaseHelper.MockUniqueConstraint<TestEntity>(
                x => x.Name,
                nameof(TestEntity.Name),
                collectionName: collectionName,
                conflictResolution: lower,
                nullValueHandling: default);


            var repo = DatabaseHelper.SetupRepo<TestEntity, Guid>(database, x => x.Id, collectionName,
                new IUniqueConstraint<TestEntity>[] { uniqueConstraintHigher, uniqueConstraintLower });

            repo.RenewCache();

            var createEntity = () => repo.CreateMany(new[] {
                new TestEntity()
                {
                    Id = Guid.NewGuid(),
                    Name = "Peter",
                    Age = 77,
                    CarNumberPlate = "ABXD"
                }
            });

            switch (higher)
            {
                case ConflictResolution.Fail:
                    createEntity.Should().Throw<ConstraintViolationException>()
                    .Where(e => e.PropertyName.Equals(nameof(TestEntity.CarNumberPlate)));
                    break;
                case ConflictResolution.Ignore:
                    createEntity().Should().Be(0);
                    break;
            }
        }

        //[Fact]
        //public void UniqueConstraintValidator_test()
        //{
        //    Dictionary<Guid, TestEntity> testEntities = new List<TestEntity>()
        //    {
        //        // put entities here
        //    }.ToDictionary(x => x.Id);

        //    const string collectionName = "TestEntity";

        //    var uniqueConstraint = DatabaseHelper.MockUniqueConstraint<TestEntity>(
        //        x => x.CarNumberPlate,
        //        nameof(TestEntity.CarNumberPlate),
        //        collectionName: collectionName,
        //        conflictResolution: default,
        //        nullValueHandling: NullValueHandling.Ignore);

        //    var repo = Substitute.For<ICachedRepository<TestEntity, Guid>>();

        //    repo.GetKey(Arg.Any<TestEntity>()).Returns(x => x.Arg<TestEntity>().Id);
        //    //repo.FindById(Arg.Any<Guid>()).Returns(x => testEntities.Find(e => e.Id == x.Arg<Guid>()));

        //    UniqueIndex<TestEntity, Guid> idx = new(uniqueConstraint, testEntities);

        //    var sut = new CachedRepository<TestEntity, Guid>.ConstraintViolationHandler(repo, Substitute.For<ILogger>(), RepositoryOperation.Create);

        //    sut.CreateIndices
        //}
    }
}