using Xunit;
using Moneyes.Data;
using System;
using LiteDB;
using FluentAssertions;
using System.Collections.Generic;
using System.Linq;

namespace Moneyes.Test
{
    public class RepositoryDependency_CollectionTests :
        RepositoryDependency_CommonTests<RepositoryDependency_CollectionTests.Entity, RepositoryDependency_CollectionTests.OtherEntity, Guid>
    {
        public class Entity
        {
            public Guid Id { get; set; }
            public List<OtherEntity> Other { get; set; }
        }

        public class OtherEntity
        {
            public Guid Id { get; set; }
        }

        public RepositoryDependency_CollectionTests() : base(x => x.Id, x => x.Other) { }

        private static Entity CreateEntity(List<OtherEntity> otherEntities = null)
        {
            return new Entity()
            {
                Id = Guid.NewGuid(),
                Other = otherEntities
            };
        }

        private static OtherEntity CreateOtherEntity()
        {
            return new OtherEntity()
            {
                Id = Guid.NewGuid()
            };
        }

        [Fact]
        public void Test_Types_and_Names() => base.Test_Types_and_Names_General();

        [Fact]
        public void GetDependents_Should_Return_All_Dependents()
        {
            var dependency = CreateDependency();

            var otherTestEntities = new List<OtherEntity>() {
                CreateOtherEntity(),
                CreateOtherEntity()
            };

            var testEntity = CreateEntity(otherTestEntities);

            var dependents = dependency.GetDependentsOf(testEntity).Cast<OtherEntity>();

            dependents.Should().HaveCount(otherTestEntities.Count);
            dependents.Should().ContainInOrder(otherTestEntities);
        }

        [Fact]
        public void Remove_should_remove_correct_dependent()
        {
            var dependency = CreateDependency();

            var firstOtherEntity = new OtherEntity() { Id = Guid.NewGuid() };
            var secondOtherEntity = new OtherEntity() { Id = Guid.NewGuid() };

            var otherTestEntities = new List<OtherEntity>() {
                firstOtherEntity,
                secondOtherEntity
            };

            var testEntity = CreateEntity(otherTestEntities);
            

            dependency.RemoveDependents(testEntity, firstOtherEntity.Id);

            testEntity.Other.Should().BeSameAs(otherTestEntities);

            otherTestEntities.Should().ContainSingle();
            otherTestEntities.Single().Should().BeSameAs(secondOtherEntity);
        }

        [Fact]
        public void NeedsRefresh_should_return_true_for_correct_id()
        {
            var dependency = CreateDependency();

            var firstOtherEntity = new OtherEntity() { Id = Guid.NewGuid() };
            var secondOtherEntity = new OtherEntity() { Id = Guid.NewGuid() };

            var otherTestEntities = new List<OtherEntity>() {
                firstOtherEntity,
                secondOtherEntity
            };

            var testEntity = CreateEntity(otherTestEntities);

            dependency.NeedsRefresh(firstOtherEntity.Id, testEntity).Should().BeTrue();
            dependency.NeedsRefresh(secondOtherEntity.Id, testEntity).Should().BeTrue();
        }

        [Fact]
        public void NeedsRefresh_should_return_false_for_wrong_id()
        {
            var dependency = CreateDependency();

            var firstOtherEntity = new OtherEntity() { Id = Guid.NewGuid() };
            var secondOtherEntity = new OtherEntity() { Id = Guid.NewGuid() };

            var otherTestEntities = new List<OtherEntity>() {
                firstOtherEntity,
                secondOtherEntity
            };

            var testEntity = CreateEntity(otherTestEntities);

            dependency.NeedsRefresh(Guid.NewGuid(), testEntity).Should().BeFalse();
        }       

        [Fact]
        public void Replace_should_replace_only_correct_dependent()
        {
            var dependency = CreateDependency();

            var firstOtherEntity = new OtherEntity() { Id = Guid.NewGuid() };
            var secondOtherEntity = new OtherEntity() { Id = Guid.NewGuid() };

            var otherEntities = new List<OtherEntity>() {
                firstOtherEntity,
                secondOtherEntity
            };

            var entity = CreateEntity(otherEntities);

            var newOtherEntity = new OtherEntity()
            {
                Id = firstOtherEntity.Id
            };

            dependency.ReplaceDependent(entity, newOtherEntity.Id, newOtherEntity);

            entity.Other.Should().BeSameAs(otherEntities);
            entity.Other.Should().HaveCount(2);
            entity.Other.Should().Contain(newOtherEntity);
            entity.Other.Should().Contain(secondOtherEntity);
        }

        [Fact]
        public void Replace_should_do_nothing_for_wrong_key()
        {
            var dependency = CreateDependency();

            var firstOtherEntity = new OtherEntity() { Id = Guid.NewGuid() };
            var secondOtherEntity = new OtherEntity() { Id = Guid.NewGuid() };

            var otherEntities = new List<OtherEntity>() {
                firstOtherEntity,
                secondOtherEntity
            };

            var entity = CreateEntity(otherEntities);

            var newOtherEntity = CreateOtherEntity();

            dependency.ReplaceDependent(entity, newOtherEntity.Id, newOtherEntity);

            entity.Other.Should().BeSameAs(otherEntities);
            entity.Other.Should().HaveCount(2);
            entity.Other[0].Should().BeSameAs(firstOtherEntity);
            entity.Other[1].Should().BeSameAs(secondOtherEntity);
        }
    }
}