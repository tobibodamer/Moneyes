using Xunit;
using System;
using System.Linq.Expressions;
using LiteDB;
using FluentAssertions;
using NSubstitute;
using System.Linq;
using Moneyes.Data;

namespace Moneyes.Test
{
    public class RepositoryDependency_SingleTests
        : RepositoryDependency_CommonTests<RepositoryDependency_SingleTests.Entity, RepositoryDependency_SingleTests.OtherEntity, Guid>
    {
        public class Entity
        {
            public Guid Id { get; set; }
            public OtherEntity Other { get; set; }
        }

        public class OtherEntity
        {
            public Guid Id { get; set; }
        }

        public RepositoryDependency_SingleTests() : base(x => x.Id, x => x.Other) { }

        private static Entity CreateEntity(OtherEntity entity)
        {
            return new Entity()
            {
                Id = Guid.NewGuid(),
                Other = entity
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
        public void GetDependents_Should_Return_Single_Dependent()
        {
            var dependency = CreateDependency();
            var otherTestEntity = CreateOtherEntity();
            var testEntity = CreateEntity(otherTestEntity);

            var dependents = dependency.GetDependentsOf(testEntity).Cast<OtherEntity>();

            dependents.Should().ContainSingle();
            dependents.Single().Should().BeSameAs(otherTestEntity);
        }

        [Fact]
        public void Apply_should_call_Include_and_return_Collection()
        {
            var dependency = CreateDependency();

            var liteCollection = Substitute.For<ILiteCollection<Entity>>();
            liteCollection.Include(Arg.Any<Expression<Func<Entity, OtherEntity>>>()).Returns(liteCollection);

            var collection = dependency.Apply(liteCollection);

            liteCollection.Include(Arg.Is(ReferenceSelectorExpression)).Received(1);
            collection.Should().BeSameAs(liteCollection);
        }

        [Fact]
        public void Remove_should_set_dependent_null()
        {
            var dependency = CreateDependency();
            var otherTestEntity = CreateOtherEntity();
            var testEntity = CreateEntity(otherTestEntity);

            dependency.RemoveDependents(testEntity, otherTestEntity.Id);

            testEntity.Other.Should().BeNull();
        }

        [Fact]
        public void Remove_should_do_nothing_for_wrong_key()
        {
            var dependency = CreateDependency();
            var otherTestEntity = CreateOtherEntity();
            var testEntity = CreateEntity(otherTestEntity);

            dependency.RemoveDependents(testEntity, Guid.Empty);

            testEntity.Other.Should().BeSameAs(otherTestEntity);
        }

        [Fact]
        public void NeedsRefresh_should_return_true_for_correct_id()
        {
            var dependency = CreateDependency();
            var otherTestEntity = CreateOtherEntity();
            var testEntity = CreateEntity(otherTestEntity);

            dependency.NeedsRefresh(otherTestEntity.Id, testEntity).Should().BeTrue();
        }

        [Fact]
        public void NeedsRefresh_should_return_false_for_wrong_id()
        {
            var dependency = CreateDependency();
            var otherTestEntity = CreateOtherEntity();
            var testEntity = CreateEntity(otherTestEntity);

            dependency.NeedsRefresh(Guid.NewGuid(), testEntity).Should().BeFalse();
        }
                
        [Fact]
        public void Replace_with_wrong_type_should_throw()
        {
            var dependency = CreateDependency();

            var update = () => dependency.ReplaceDependent(CreateEntity(null), Guid.Empty, new object());

            update.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Replace_should_replace_property()
        {
            var dependency = CreateDependency();

            var otherEntity = CreateOtherEntity();
            var entity = CreateEntity(otherEntity);

            var newOtherEntity = new OtherEntity()
            {
                Id = otherEntity.Id
            };

            dependency.ReplaceDependent(entity, newOtherEntity.Id, newOtherEntity);

            entity.Other.Should().BeSameAs(newOtherEntity);
        }

        [Fact]
        public void Replace_should_do_nothing_for_wrong_key()
        {
            var dependency = CreateDependency();

            var otherEntity = CreateOtherEntity();
            var entity = CreateEntity(otherEntity);

            var newOtherEntity = CreateOtherEntity();

            dependency.ReplaceDependent(entity, newOtherEntity.Id, newOtherEntity);

            entity.Other.Should().BeSameAs(otherEntity);
        }
    }
}