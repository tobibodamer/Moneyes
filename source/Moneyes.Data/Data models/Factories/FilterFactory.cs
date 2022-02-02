using Moneyes.Core;
using Moneyes.Core.Filters;
using System.Linq;

namespace Moneyes.Data
{
    public class FilterFactory : IFilterFactory
    {
        public TransactionFilter CreateTransactionFilterFromDto(TransactionFilterDto transactionFilterDto)
        {
            return new()
            {
                TransactionType = transactionFilterDto.TransactionType,
                StartDate = transactionFilterDto.StartDate,
                EndDate = transactionFilterDto.EndDate,
                AccountNumber = transactionFilterDto.AccountNumber,
                MaxAmount = transactionFilterDto.MaxAmount,
                MinAmount = transactionFilterDto.MinAmount,
                Criteria = CreateFilterGroupFromDto<Transaction>(transactionFilterDto.Criteria)
            };
        }

        public FilterGroup<T> CreateFilterGroupFromDto<T>(FilterGroupDto filterGroupDto)
        {
            if (filterGroupDto == null)
            {
                return null;
            }

            return new(filterGroupDto.Operator)
            {
                ChildFilters = filterGroupDto.ChildFilters?.Select(f => CreateFilterGroupFromDto<T>(f)).ToList() ?? null,
                Conditions = filterGroupDto.Conditions?.Select(c =>
                {
                    var conditionFilter = ConditionFilters.Create<T>(c.Selector);

                    conditionFilter.CompareAll = c.CompareAll;
                    conditionFilter.CaseSensitive = c.CaseSensitive;
                    conditionFilter.Operator = c.Operator;
                    conditionFilter.Values = c.Values;

                    return conditionFilter;
                }).ToList() ?? null
            };
        }
    }
}
