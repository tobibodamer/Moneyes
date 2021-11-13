using Newtonsoft.Json;
using System;
using System.Collections;

namespace Moneyes.Core.Filters
{
    public class TransactionFilter : IEvaluable<Transaction>
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public TransactionType TransactionType { get; set; }
        public double? TotalDays
        {
            get
            {
                if (StartDate is not null && EndDate is not null)
                {
                    return (EndDate - StartDate).Value.TotalDays + 1;
                }

                return null;
            }
        }
#nullable enable
        public string? AccountNumber { get; set; }
#nullable disable

        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }

        public FilterGroup<Transaction> Criteria { get; set; } = new();

        public static TransactionFilter Create(
            DateTime? startDate = null,
            DateTime? endDate = null,
            TransactionType saleType = TransactionType.None,
            string accountNumber = null)
        {
            TransactionFilter filter = new()
            {
                StartDate = startDate,
                EndDate = endDate,
                TransactionType = saleType,
                AccountNumber = accountNumber
            };

            return filter;
        }

        public bool Evaluate(Transaction input)
        {
            return (TransactionType is TransactionType.None || (input.Type == TransactionType))
               && (!StartDate.HasValue || (input.BookingDate >= StartDate))
               && (!EndDate.HasValue || (input.BookingDate <= EndDate))
               && (AccountNumber is null || (input.IBAN?.EndsWith(AccountNumber) ?? true))
               && (MinAmount is null || Math.Abs(input.Amount) >= MinAmount)
               && (MaxAmount is null || Math.Abs(input.Amount) <= MaxAmount)
               && (Criteria is null || Criteria.Evaluate(input));
        }

        public bool IsNull()
        {
            return TransactionType is TransactionType.None &&
                StartDate is null &&
                EndDate is null &&
                AccountNumber is null &&
                MinAmount is null &&
                MaxAmount is null &&
                (Criteria is null || (Criteria.ChildFilters.Count == 0 && Criteria.Conditions.Count == 0));
        }
    }
}