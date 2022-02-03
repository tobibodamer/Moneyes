using Moneyes.Core;
using Moneyes.Core.Filters;
using System;
using System.Linq;

namespace Moneyes.Data
{
    public static class DboConversionExtensions
    {
        public static TransactionDbo ToDbo(this Transaction transaction,
            DateTime? createdAt = null, DateTime? updatedAt = null, bool isDeleted = false)
        {
            return new TransactionDbo(transaction.Id, transaction.UID, createdAt, updatedAt, isDeleted)
            {                
                AltName = transaction.AltName,
                Amount = transaction.Amount,
                IBAN = transaction.IBAN,
                BIC = transaction.BIC,
                BookingDate = transaction.BookingDate,
                BookingType = transaction.BookingType,
                Category = transaction.Category?.ToDbo(),
                Currency = transaction.Currency,
                ValueDate = transaction.ValueDate,
                Index = transaction.Index,
                Name = transaction.Name,
                PartnerIBAN = transaction.PartnerIBAN,
                Purpose = transaction.Purpose,
            };
        }
        public static CategoryDbo ToDbo(this Category category,
            DateTime? createdAt = null, DateTime? updatedAt = null, bool isDeleted = false)
        {
            return new(category.Id, category.Name, createdAt, updatedAt, isDeleted)
            {
                Filter = category.Filter?.ToDto(),
                IsExlusive = category.IsExlusive,
                Parent = category.Parent?.ToDbo(),
                Target = category.Target,
            };
        }

        public static AccountDbo ToDbo(this AccountDetails account,
            DateTime? createdAt = null, DateTime? updatedAt = null, bool isDeleted = false)
        {
            return new(account.Id, account.Number, createdAt, updatedAt, isDeleted)
            {           
                Bank = account.BankDetails.ToDbo(),
                IBAN = account.IBAN,                
                OwnerName = account.OwnerName,
                Type = account.Type,
                Permissions = account.Permissions?.ToList()
            };
        }

        public static BankDbo ToDbo(this BankDetails bankDetails,
            DateTime? createdAt = null, DateTime? updatedAt = null, bool isDeleted = false)
        {
            return new(bankDetails.Id, bankDetails.BankCode, createdAt, updatedAt, isDeleted)
            {
                Name = bankDetails.Name,                
                BIC = bankDetails.BIC,
                HbciVersion = bankDetails.HbciVersion,
                Server = bankDetails.Server,
                UserId = bankDetails.UserId,
                Pin = bankDetails.Pin,
            };
        }

        public static BalanceDbo ToDbo(this Balance balance,
            DateTime? createdAt = null, DateTime? updatedAt = null, bool isDeleted = false)
        {
            return new(balance.Id, balance.Date, balance.Amount, createdAt, updatedAt, isDeleted)
            {                
                Currency = balance.Currency,
                Account = balance.Account.ToDbo(),
            };
        }

        public static TransactionFilterDto ToDto(this TransactionFilter transactionFilter)
        {
            return new TransactionFilterDto()
            {
                StartDate = transactionFilter.StartDate,
                EndDate = transactionFilter.EndDate,
                AccountNumber = transactionFilter.AccountNumber,
                MaxAmount = transactionFilter.MaxAmount,
                MinAmount = transactionFilter.MinAmount,
                TransactionType = transactionFilter.TransactionType,
                Criteria = transactionFilter.Criteria.ToDto()
            };
        }

        private static FilterGroupDto ToDto<T>(this FilterGroup<T> filterGroup)
        {
            return new()
            {
                Operator = filterGroup.Operator,
                ChildFilters = filterGroup.ChildFilters.Select(c => c.ToDto()).ToList(),
                Conditions = filterGroup.Conditions.Select(c => c.ToDto()).ToList()
            };
        }

        private static ConditionFilterDto ToDto<T>(this IConditionFilter<T> conditionFilter)
        {
            return new()
            {
                Operator = conditionFilter.Operator,
                CaseSensitive = conditionFilter.CaseSensitive,
                CompareAll = conditionFilter.CompareAll,
                Selector = conditionFilter.Selector,
                Values = conditionFilter.Values.Cast<object>().ToArray(),
            };
        }
    }
}
