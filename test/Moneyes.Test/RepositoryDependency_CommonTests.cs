using Xunit;
using Moneyes.Data;
using System;
using System.Linq.Expressions;
using FluentAssertions;
using System.Collections.Generic;
using NSubstitute;
using System.Linq;

namespace Moneyes.Test
{
    public abstract class RepositoryDependency_CommonTests<TEntity, TOtherEntity, TOtherKey>
        where TOtherEntity : class
    {
        protected readonly Expression<Func<TOtherEntity, TOtherKey>> OtherKeySelectorExpression;
        protected readonly Expression<Func<TEntity, TOtherEntity>> ReferenceSelectorExpression;
        private readonly Expression<Func<TEntity, ICollection<TOtherEntity>>> CollectionReferenceSelectorExpression;

        protected bool IsCollectionTests { get; }
        public RepositoryDependency_CommonTests(Expression<Func<TOtherEntity, TOtherKey>> otherKeySelector,
            Expression<Func<TEntity, TOtherEntity>> referenceSelector)
        {
            OtherKeySelectorExpression = otherKeySelector;
            ReferenceSelectorExpression = referenceSelector;
            IsCollectionTests = false;
        }

        public RepositoryDependency_CommonTests(Expression<Func<TOtherEntity, TOtherKey>> otherKeySelector,
            Expression<Func<TEntity, ICollection<TOtherEntity>>> referenceSelector)
        {
            OtherKeySelectorExpression = otherKeySelector;
            CollectionReferenceSelectorExpression = referenceSelector;
            IsCollectionTests = true;
        }

        private const string TargetCollectionName = "TargetCollection";
        private const string SourceCollectionName = "SourceCollection";

        public static ICachedRepository<T> MockRepoWithGetKey<T, TKey>(Func<T, TKey> keySelector)
        {
            var repo = Substitute.For<ICachedRepository<T>>();

            repo.GetKey(Arg.Any<T>()).Returns(x => keySelector(x.Arg<T>()));

            return repo;
        }
        protected IRepositoryDependency<TEntity> CreateDependency()
        {
            var otherRepo = MockRepoWithGetKey<TOtherEntity, TOtherKey>(OtherKeySelectorExpression.Compile());

            var repositoryProvider = Substitute.For<IRepositoryProvider>();
            repositoryProvider.GetRepository<TOtherEntity>(Arg.Any<string>()).Returns(otherRepo);


            if (IsCollectionTests)
            {
                return new RepositoryDependency<TEntity, TOtherEntity>(
                    repositoryProvider,
                    CollectionReferenceSelectorExpression,
                    TargetCollectionName,
                    SourceCollectionName,
                    () => Enumerable.Empty<IRepositoryDependency<TOtherEntity>>());
            }
            else
            {
                return new RepositoryDependency<TEntity, TOtherEntity>(
                    repositoryProvider,
                    ReferenceSelectorExpression,
                    TargetCollectionName,
                    SourceCollectionName,
                    () => Enumerable.Empty<IRepositoryDependency<TOtherEntity>>());
            }
        }

        [Fact]
        public void Test_Types_and_Names_General()
        {
            var dependency = CreateDependency();

            dependency.SourceType.Should().Be(typeof(TOtherEntity));
            dependency.TargetType.Should().Be(typeof(TEntity));
            dependency.SourceCollectionName.Should().Be("SourceCollection");
            dependency.TargetCollectionName.Should().Be("TargetCollection");
            dependency.HasMultipleDependents.Should().Be(IsCollectionTests);

            LambdaExpression expr = IsCollectionTests ? CollectionReferenceSelectorExpression : ReferenceSelectorExpression;

            dependency.PropertyName.Should().Be(expr.Body.As<MemberExpression>().Member.Name);
        }
    }
}