using Newtonsoft.Json;
using System;
using System.Collections;

namespace Moneyes.Core.Filters
{
    public class SalesFilter
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public SaleType? SaleType { get; set; }
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

        public FilterGroup<ISale> Criteria { get; set; } = new();

        public static SalesFilter Create(DateTime? startDate = null, 
            DateTime? endDate = null, SaleType? saleType = null)
        {
            var filter = new SalesFilter
            {
                StartDate = startDate,
                EndDate = endDate,
                SaleType = saleType
            };

            return filter;
        }
    }

}
