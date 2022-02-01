using Moneyes.Core.Filters;

namespace Moneyes.Data
{
    public interface IFilterFactory
    {
        TransactionFilter CreateTransactionFilterFromDto(TransactionFilterDto transactionFilterDto);
        FilterGroup<T> CreateFilterGroupFromDto<T>(FilterGroupDto filterGroupDto);
    }
}
