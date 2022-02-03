using Moneyes.Core;
using Moneyes.Core.Filters;
using System;
using System.Linq;

namespace Moneyes.Data
{
    public static class Extensions
    {
        public static TransactionDbo ToDbo(this Transaction transaction,
            DateTime? createdAt = null, DateTime? updatedAt = null, bool isDeleted = false)
        {
            return new TransactionDbo()
            {
                IsDeleted = isDeleted,
                CreatedAt = createdAt ?? DateTime.MinValue,
                UpdatedAt = updatedAt ?? DateTime.MinValue,
                AltName = transaction.AltName,
                Amount = transaction.Amount,
                IBAN = transaction.IBAN,
                BIC = transaction.BIC,
                BookingDate = transaction.BookingDate,
                BookingType = transaction.BookingType,
                Category = transaction.Category?.ToDbo(),
                Currency = transaction.Currency,
                UID = transaction.UID,
                ValueDate = transaction.ValueDate,
                Id = transaction.Id,
                Index = transaction.Index,
                Name = transaction.Name,
                PartnerIBAN = transaction.PartnerIBAN,
                Purpose = transaction.Purpose,
            };
        }
        public static CategoryDbo ToDbo(this Category category,
            DateTime? createdAt = null, DateTime? updatedAt = null, bool isDeleted = false)
        {
            return new()
            {
                IsDeleted = isDeleted,
                CreatedAt = createdAt ?? DateTime.MinValue,
                UpdatedAt = updatedAt ?? DateTime.MinValue,
                Id = category.Id,
                Filter = category.Filter?.ToDto(),
                IsExlusive = category.IsExlusive,
                Name = category.Name,
                Parent = category.Parent?.ToDbo(),
                Target = category.Target,
            };
        }

        public static AccountDbo ToDbo(this AccountDetails account,
            DateTime? createdAt = null, DateTime? updatedAt = null, bool isDeleted = false)
        {
            return new()
            {
                IsDeleted = isDeleted,
                CreatedAt = createdAt ?? DateTime.MinValue,
                UpdatedAt = updatedAt ?? DateTime.MinValue,
                Id = account.Id,
                Bank = account.BankDetails.ToDbo(),
                IBAN = account.IBAN,
                Number = account.Number,
                OwnerName = account.OwnerName,
                Type = account.Type,
                Permissions = account.Permissions?.ToList()
            };
        }

        public static BankDbo ToDbo(this BankDetails bankDetails,
            DateTime? createdAt = null, DateTime? updatedAt = null, bool isDeleted = false)
        {
            return new()
            {
                IsDeleted = isDeleted,
                CreatedAt = createdAt ?? DateTime.MinValue,
                UpdatedAt = updatedAt ?? DateTime.MinValue,
                Id = bankDetails.Id,
                Name = bankDetails.Name,
                BankCode = bankDetails.BankCode,
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
            return new()
            {
                IsDeleted = isDeleted,
                CreatedAt = createdAt ?? DateTime.MinValue,
                UpdatedAt = updatedAt ?? DateTime.MinValue,
                Id = balance.Id,
                Date = balance.Date,
                Amount = balance.Amount,
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
