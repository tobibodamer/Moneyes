using FluentAssertions;
using Moneyes.Core;
using Xunit;

namespace Moneyes.Test
{
    public class TransactionServiceTests : TransactionServiceTestBase
    {
        [Theory]
        [InlineData(0, 0)] // null -> 0
        [InlineData(1, 0)] // 1 -> 0
        [InlineData(1, 1)] // 1 -> 1
        public void MoveToCategory(int transactionIndex, int targetCategory)
        {
            // Category should not change if previous and new are null
            var transaction = _transactions[transactionIndex];
            var expectedCategory = _categories[targetCategory];
            var expectedResult = transaction.Category != expectedCategory;

            var result = _transactionService.MoveToCategory(transaction, _categories[targetCategory]);
                        
            transaction.Category.Should().Be(expectedCategory);
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0)] // null -> null
        [InlineData(1)] // 1 -> null
        public void MoveToCategory_NoCategory_shouldSetCategoryToNull(int transactionIndex)
        {
            var transaction = _transactions[transactionIndex];
            var expectedResult = transaction.Category != null;

            var result = _transactionService.MoveToCategory(transaction, Category.NoCategory);

            transaction.Category.Should().BeNull();
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0)] // null -> null
        [InlineData(1)] // 1 -> 1        
        public void MoveToCategory_AllCategory_shouldDoNothing(int transactionIndex)
        {            
            var transaction = _transactions[transactionIndex];
            var expectedCategory = transaction.Category;

            var result = _transactionService.MoveToCategory(transaction, Category.AllCategory);

            transaction.Category.Should().Be(expectedCategory);
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(0)] // null -> null
        [InlineData(1)] // 1 -> null
        [InlineData(2)] // 0 -> null
        public void RemoveFromCategory_shouldSetCategoryToNull(int transactionIndex)
        {
            var transaction = _transactions[transactionIndex];
            var expectedResult = transaction.Category != null;

            var result = _transactionService.RemoveFromCategory(transaction);

            transaction.Category.Should().BeNull();
            result.Should().Be(expectedResult);
        }
    }
}
