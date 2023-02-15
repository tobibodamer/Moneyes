using FluentAssertions;
using FluentAssertions.Execution;
using LiteDB;
using Moneyes.Core;
using Moneyes.LiveData;
using Moneyes.UI;
using NSubstitute.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Moneyes.Test
{
    public class CategoryAssignmentTests : TransactionServiceTestBase
    {

        [Theory]
        [MemberData(nameof(GetData_KeepPrevious))]
        public void AssignCategory_KeepPrevious_shouldSetCorrectCategory(int transactionIndex, int? categoryIndex, int? expectedCategoryIndex)
        {
            AssignCategory(transactionIndex, categoryIndex, expectedCategoryIndex, AssignMethod.KeepPrevious);
        }


        [Theory]
        [MemberData(nameof(GetData_KeepPreviousAlways))]
        public void AssignCategory_KeepPreviousAlways_shouldSetCorrectCategory(int transactionIndex, int? categoryIndex, int? expectedCategoryIndex)
        {
            AssignCategory(transactionIndex, categoryIndex, expectedCategoryIndex, AssignMethod.KeepPreviousAlways);
        }

        [Theory]
        [MemberData(nameof(GetData_Simple))]
        public void AssignCategory_Simple_shouldSetCorrectCategory(int transactionIndex, int? categoryIndex, int? expectedCategoryIndex)
        {
            AssignCategory(transactionIndex, categoryIndex, expectedCategoryIndex, AssignMethod.Simple);
        }

        [Theory]
        [MemberData(nameof(GetData_Reset))]
        public void AssignCategory_Reset_shouldSetCorrectCategory(int transactionIndex, int? categoryIndex, int? expectedCategoryIndex)
        {
            AssignCategory(transactionIndex, categoryIndex, expectedCategoryIndex, AssignMethod.Reset);
        }


        private void AssignCategory(int transactionIndex, int? categoryIndex, int? expectedCategoryIndex, AssignMethod assignMethod)
        {
            // Category should not change if previous and new are null
            var transaction = new Transaction(Guid.NewGuid())
            {
                Category = categoryIndex != null ? _categories[categoryIndex.Value] : null,
                Index = transactionIndex
            };

            _transactionService.AssignCategory(transaction, assignMethod);

            if (expectedCategoryIndex == null)
            {
                transaction.Category.Should().BeNull();
            }
            else
            {
                transaction.Category.Should().Be(_categories[expectedCategoryIndex.Value]);
            }
        }

        public static IEnumerable<object[]> GetData_Simple()
            => GetIndices_Simple().Select(x => new object[] { x.transactionIndex, x.categoryIndex, x.expectedCategoryIndex });

        public static IEnumerable<object[]> GetData_KeepPrevious()
            => GetIndices_KeepPrevious().Select(x => new object[] { x.transactionIndex, x.categoryIndex, x.expectedCategoryIndex });

        public static IEnumerable<object[]> GetData_KeepPreviousAlways()
            => GetIndices_KeepPreviousAlways().Select(x => new object[] { x.transactionIndex, x.categoryIndex, x.expectedCategoryIndex });

        public static IEnumerable<object[]> GetData_Reset()
            => GetIndices_Reset().Select(x => new object[] { x.transactionIndex, x.categoryIndex, x.expectedCategoryIndex });


        private static IEnumerable<(int transactionIndex, int? categoryIndex, int? expectedCategoryIndex)> GetIndices_Simple()
        {
            yield return (0, null, null); // Category should not change, because no category matches
            yield return (0, 0, 0);  // Category should not change, because no category matches
            yield return (1, null, 1); // Category should change to matching (1)
            yield return (1, 1, 1); // Category should change to matching (1)
            yield return (2, null, 2); // Category should change to matching (2)
            yield return (2, 1, 2); // Category should change to matching (2)
            yield return (3, null, null); // Category should not change, because no category matches
            yield return (3, 1, 1); // Category should not change, because no category matches
            yield return (4, null, 2); // Category should change to matching (2)
            yield return (4, 0, 2); // Category should change to matching (2)
            yield return (500, null, 3); // New transaction should be assigned to matching (3)
            yield return (500, 0, 3); // New transaction should be assigned to matching (3)
            yield return (500, 1, 3); // New transaction should be assigned to matching (3)
            yield return (99, null, null); // New transaction should not change category, because no category matches
            yield return (99, 0, 0); // New transaction should not change category, because no category matches
            yield return (99, 1, 1); // New transaction should not change category, because no category matches
        }

        private static IEnumerable<(int transactionIndex, int? categoryIndex, int? expectedCategoryIndex)> GetIndices_KeepPrevious()
        {
            yield return (0, null, null); // Category should not change if previous category is null and no category matches
            yield return (0, 0, 0);  // Category should not change if previous category is null and no category matches
            yield return (1, null, 1); // Keep previous
            yield return (1, 1, 1); // Keep previous
            yield return (2, null, 0); // Keep previous
            yield return (2, 1, 0); // Keep previous
            yield return (3, null, 2); // Keep previous
            yield return (3, 1, 2); // Keep previous
            yield return (4, null, 2); // Change to matching, because previous category is null
            yield return (4, 0, 2); // Change to matching, because previous category is null
            yield return (500, null, 3); // New transaction should be assigned to matching (3)
            yield return (500, 0, 3); // New transaction should be assigned to matching (3)
            yield return (500, 1, 3); // New transaction should be assigned to matching (3)
            yield return (99, null, null); // New transaction should not change category, because no category matches
            yield return (99, 0, 0); // New transaction should not change category, because no category matches
            yield return (99, 1, 1); // New transaction should not change category, because no category matches
        }

        private static IEnumerable<(int transactionIndex, int? categoryIndex, int? expectedCategoryIndex)> GetIndices_KeepPreviousAlways()
        {
            yield return (0, null, null); // Category should change to previous (null)
            yield return (0, 0, null);  // Category should change to previous (null)
            yield return (1, null, 1); // Keep previous
            yield return (1, 1, 1); // Keep previous
            yield return (2, null, 0); // Keep previous
            yield return (2, 1, 0); // Keep previous
            yield return (3, null, 2); // Keep previous
            yield return (3, 1, 2); // Keep previous
            yield return (4, null, null); // Category should change to previous (null)
            yield return (4, 0, null); // Category should change to previous (null)
            yield return (500, null, 3); // New transaction should be assigned to matching (3)
            yield return (500, 0, 3); // New transaction should be assigned to matching (3)
            yield return (500, 1, 3); // New transaction should be assigned to matching (3)
            yield return (99, null, null); // New transaction should not change category, because no category matches
            yield return (99, 0, 0); // New transaction should not change category, because no category matches
            yield return (99, 1, 1); // New transaction should not change category, because no category matches
        }

        private static IEnumerable<(int transactionIndex, int? categoryIndex, int? expectedCategoryIndex)> GetIndices_Reset()
        {
            yield return (0, null, null); // Category should not change, because no category matches
            yield return (0, 0, null);  // Category should be reset, because no category matches
            yield return (0, 1, null);  // Category should be reset, because no category matches
            yield return (1, null, 1); // Category should change to matching (1)
            yield return (1, 0, 1); // Category should change to matching (1)
            yield return (1, 1, 1); // Category should change to matching (1)
            yield return (2, null, 2); // Category should change to matching (2)
            yield return (2, 0, 2); // Category should change to matching (2)
            yield return (2, 1, 2); // Category should change to matching (2)
            yield return (3, null, null); // Category should not change, because no category matches
            yield return (3, 0, null); // Category should not change, because no category matches
            yield return (3, 1, null); // Category should be reset, because no category matches
            yield return (4, null, 2); // Category should change to matching (2)
            yield return (4, 0, 2); // Category should change to matching (2)
            yield return (4, 1, 2); // Category should change to matching (2)
            yield return (500, null, 3); // New transaction should be assigned to matching (3)
            yield return (500, 0, 3); // New transaction should be assigned to matching (3)
            yield return (500, 1, 3); // New transaction should be assigned to matching (3)
            yield return (99, null, null); // New transaction should not change category, because no category matches
            yield return (99, 0, null); // New transaction should reset category, because no category matches
            yield return (99, 1, null); // New transaction should reset category, because no category matches
        }

        private static IEnumerable<Transaction> CreateTransactionsFromIndices(
            IEnumerable<(int transactionIndex, int? categoryIndex)> indeces,
            IReadOnlyList<Category> categories)
        {
            foreach (var (transactionIndex, categoryIndex) in indeces)
            {
                yield return new Transaction(Guid.NewGuid())
                {
                    Category = categoryIndex != null ? categories[categoryIndex.Value] : null,
                    Index = transactionIndex
                };
            }
        }

        [Fact]
        public void AssignCategories_Simple_shouldSetCorrectCategories()
        {
            AssignCategories(GetIndices_Simple().ToArray(), AssignMethod.Simple);
        }

        [Fact]
        public void AssignCategories_KeepPrevious_shouldSetCorrectCategories()
        {
            AssignCategories(GetIndices_KeepPrevious().ToArray(), AssignMethod.KeepPrevious);
        }

        [Fact]
        public void AssignCategories_KeepPreviousAlways_shouldSetCorrectCategories()
        {
            AssignCategories(GetIndices_KeepPreviousAlways().ToArray(), AssignMethod.KeepPreviousAlways);
        }

        [Fact]
        public void AssignCategories_Reset_shouldSetCorrectCategories()
        {
            AssignCategories(GetIndices_Reset().ToArray(), AssignMethod.Reset);
        }

        private void AssignCategories(
            (int transactionIndex, int? categoryIndex, int? expectedCategoryIndex)[] data, 
            AssignMethod assignMethod)
        {
            var transactions = CreateTransactionsFromIndices(
                indeces: data.Select((indeces) => (indeces.transactionIndex, indeces.categoryIndex)),
                _categories).ToArray();

            var expectedCategories = data.Select(x =>
                x.expectedCategoryIndex != null ? _categories[x.expectedCategoryIndex.Value] : null).ToArray();

            _transactionService.AssignCategories(transactions, assignMethod);

            transactions.Select(x => x.Category).Should().BeEquivalentTo(expectedCategories);
        }

        [Theory]
        [InlineData(AssignMethod.Simple, null, 1, 2, 2, 2)]
        [InlineData(AssignMethod.Reset, null, 1, 2, null, 2)]
        [InlineData(AssignMethod.KeepPrevious, null, 1, 0, 2, 2)]
        [InlineData(AssignMethod.KeepPreviousAlways, null, 1, 0, 2, null)]
        public void ReassignCategories_shouldSetCorrectCategories(AssignMethod assignMethod, params int?[] expectedCategoryIndeces)
        {
            _transactionService.ReassignCategories(assignMethod);

            var expectedCategories = expectedCategoryIndeces.Select<int?, Guid?>(i => i != null ? _categories[i.Value].Id : null);
            var categoriesAfterReassign = _transactionsCollection.FindAll().Select(t => t.Category?.Id);

            categoriesAfterReassign.Should().BeEquivalentTo(expectedCategories);
        }

        [Theory]
        [InlineData(AssignMethod.Simple, 2)]
        [InlineData(AssignMethod.Reset, 3)]
        [InlineData(AssignMethod.KeepPrevious, 1)]
        [InlineData(AssignMethod.KeepPreviousAlways, 0)]
        public void ReassignCategories_returnsCorrectTransactionsCount(AssignMethod assignMethod, int expectedTransactionsUpdated)
        {
            var transactionsUpdated = _transactionService.ReassignCategories(assignMethod);

            transactionsUpdated.Should().Be(expectedTransactionsUpdated);
        }

        [Theory]
        // Category 0 has no filter -> no changes
        [InlineData(AssignMethod.Simple, 0, null, 1, 0, 2, null)]
        [InlineData(AssignMethod.Reset, 0, null, 1, 0, 2, null)]
        [InlineData(AssignMethod.KeepPrevious, 0, null, 1, 0, 2, null)]
        [InlineData(AssignMethod.KeepPreviousAlways, 0, null, 1, 0, 2, null)]

        // Category 1 already assigned to transaction 1 only -> no changes
        [InlineData(AssignMethod.Simple, 1, null, 1, 0, 2, null)]
        [InlineData(AssignMethod.Reset, 1, null, 1, 0, 2, null)]
        [InlineData(AssignMethod.KeepPrevious, 1, null, 1, 0, 2, null)]
        [InlineData(AssignMethod.KeepPreviousAlways, 1, null, 1, 0, 2, null)]

        // Category 2 assigned wrongly to T3, matches T2, T4
        [InlineData(AssignMethod.Simple, 2, null, 1, 2, 2, 2)]
        [InlineData(AssignMethod.Reset, 2, null, 1, 2, null, 2)]
        [InlineData(AssignMethod.KeepPrevious, 2, null, 1, 0, 2, 2)]
        [InlineData(AssignMethod.KeepPreviousAlways, 2, null, 1, 0, 2, null)]

        // Category 3 not assigned, matches no transaction -> no changes
        [InlineData(AssignMethod.Simple, 3, null, 1, 0, 2, null)]
        [InlineData(AssignMethod.Reset, 3, null, 1, 0, 2, null)]
        [InlineData(AssignMethod.KeepPrevious, 3, null, 1, 0, 2, null)]
        [InlineData(AssignMethod.KeepPreviousAlways, 3, null, 1, 0, 2, null)]
        public void ReassignCategory_shouldSetCorrectCategories(AssignMethod assignMethod, int categoryIndex, params int?[] expectedCategoryIndeces)
        {
            _transactionService.ReassignCategory(_categories[categoryIndex], assignMethod);

            var expectedCategories = expectedCategoryIndeces.Select<int?, Guid?>(i => i != null ? _categories[i.Value].Id : null);
            var categoriesAfterReassign = _transactionsCollection.FindAll().Select(t => t.Category?.Id);

            categoriesAfterReassign.Should().BeEquivalentTo(expectedCategories);
        }

        [Theory]
        // Category 0 has no filter -> no changes
        [InlineData(AssignMethod.Simple, 0, 0)]
        [InlineData(AssignMethod.Reset, 0, 0)]
        [InlineData(AssignMethod.KeepPrevious, 0, 0)]
        [InlineData(AssignMethod.KeepPreviousAlways, 0, 0)]

        // Category 1 already assigned to transaction 1 only -> no changes
        [InlineData(AssignMethod.Simple, 1, 0)]
        [InlineData(AssignMethod.Reset, 1, 0)]
        [InlineData(AssignMethod.KeepPrevious, 1, 0)]
        [InlineData(AssignMethod.KeepPreviousAlways, 1, 0)]

        // Category 2 assigned wrongly to T3, matches T2, T4
        [InlineData(AssignMethod.Simple, 2, 2)]
        [InlineData(AssignMethod.Reset, 2, 3)]
        [InlineData(AssignMethod.KeepPrevious, 2, 1)]
        [InlineData(AssignMethod.KeepPreviousAlways, 2, 0)]

        // Category 3 not assigned, matches no transaction -> no changes
        [InlineData(AssignMethod.Simple, 3, 0)]
        [InlineData(AssignMethod.Reset, 3, 0)]
        [InlineData(AssignMethod.KeepPrevious, 3, 0)]
        [InlineData(AssignMethod.KeepPreviousAlways, 3, 0)]
        public void ReassignCategory_returnsCorrectTransactionsCount(AssignMethod assignMethod, int categoryIndex, int expectedTransactionsUpdated)
        {
            var transactionsUpdated = _transactionService.ReassignCategory(_categories[categoryIndex], assignMethod);

            _ = _transactionsCollection.FindAll().Select(t => t.Category?.Id);

            transactionsUpdated.Should().Be(expectedTransactionsUpdated);
        }


    }
}
