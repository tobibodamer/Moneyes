using Moneyes.Core;
using Moneyes.LiveData;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using static System.Formats.Asn1.AsnWriter;

namespace Moneyes.UI
{
    internal sealed class Expenditure : BaseExpenditure, IExpenditure
    {
        //Lazy<IReadOnlyDictionary<DateTime, IExpenditure>> _dailyExpensesLazy;
        //Lazy<IReadOnlyDictionary<DateTime, IExpenditure>> _monthlyExpensesLazy;
        //Lazy<IReadOnlyDictionary<DateTime, IExpenditure>> _yearlyExpensesLazy;

        //IReadOnlyDictionary<DateTime, IExpenditure> _dailyExpenses;
        //IReadOnlyDictionary<DateTime, IExpenditure> _monthlyExpenses;
        //IReadOnlyDictionary<DateTime, IExpenditure> _yearlyExpenses;

        public IReadOnlyDictionary<DateTime, IExpenditure> DailyExpenses { get; private init; }
        public IReadOnlyDictionary<DateTime, IExpenditure> MonthlyExpenses { get; private init; }
        public IReadOnlyDictionary<DateTime, IExpenditure> YearlyExpenses { get; private init; }

        //public Expenditure(IEnumerable<Transaction> transactions, bool absolute) : this(transactions, absolute, DateGroupScope.All)
        //{
        //    if (!transactions.Any())
        //    {
        //        return;
        //    }


        //    //(TotalAmount, TotalExpense, TotalIncome, StartDate, EndDate) = groupFastComplete();

        //    (StartDate, EndDate) = Transactions.Select(t => t.BookingDate).MinAndMax();

        //    (TotalAmount, TotalIncome, TotalExpense) =
        //        YearlyExpenses.Values.Aggregate((total: 0m, income: 0m, expense: 0m),
        //            (sum, expenditure) =>
        //                (
        //                 sum.total + expenditure.TotalAmount,
        //                 sum.income + expenditure.TotalIncome,
        //                 sum.expense + expenditure.TotalExpense
        //                )
        //            );
        //}

        //private Expenditure(IEnumerable<Transaction> transactions, bool absolute, bool dontGroup) : base(transactions, absolute)
        //{
        //    if (!transactions.Any())
        //    {
        //        return;
        //    }
        //}

        private static readonly IReadOnlyDictionary<DateTime, IExpenditure> EmptyDictionary
            = new ReadOnlyDictionary<DateTime, IExpenditure>(new Dictionary<DateTime, IExpenditure>());

        //void dsaddf()
        //{
        //    // https://github.com/jackmott/GroupByPerformance/blob/master/README.md


        //    var dictionary = new Dictionary<DateTime, (List<Transaction> transactions, decimal total)>();
        //    foreach (var t in Transactions)
        //    {
        //        var key = t.BookingDate.Date;
        //        if (!dictionary.TryGetValue(key, out var grouping))
        //        {
        //            grouping = (new(1), 0m);
        //            dictionary.Add(key, grouping);
        //        }
        //        grouping.transactions.Add(t);
        //        grouping.total += t.Amount;
        //    }

        //    var daily = GroupByDictionary(Transactions, t => t.BookingDate.Date, t => t).ToDictionary(kvp => kvp.Key, kvp =>
        //    {
        //        return new Expenditure(kvp.Value, IsAbsolute)
        //        {
        //            TotalAmount = dailyAcc.total,
        //            TotalExpense = dailyAcc.expense,
        //            TotalIncome = dailyAcc.income,
        //            StartDate = day,
        //            EndDate = day.AddDays(1),
        //        }
        //    });
        //}

        //public static Dictionary<K, List<V>> GroupByDictionary<U, K, V>(this IEnumerable<U> items, Func<U, K> keySelector, Func<U, V> valueSelector)
        //{
        //    var dictionary = new Dictionary<K, List<V>>();
        //    foreach (var item in items)
        //    {
        //        List<V> grouping;
        //        var key = keySelector(item);
        //        if (!dictionary.TryGetValue(key, out grouping))
        //        {
        //            grouping = new List<V>(1);
        //            dictionary.Add(key, grouping);
        //        }
        //        grouping.Add(valueSelector(item));
        //    }
        //    return dictionary;
        //}

        //(decimal total, decimal expense, decimal income, DateTime start, DateTime end) groupFastComplete()
        //{
        //    var orderedTransactions = Transactions.OrderBy(g => g.BookingDate).ToList();

        //    DateTime day = orderedTransactions[0].BookingDate.Date;
        //    (List<Transaction> transactions, decimal total, decimal expense, decimal income) dailyAcc = (new(), 0, 0, 0);
        //    (List<Transaction> transactions, decimal total, decimal expense, decimal income, Dictionary<DateTime, IExpenditure> daily) monthlyAcc = (new(), 0, 0, 0, new());
        //    (List<Transaction> transactions, decimal total, decimal expense, decimal income,
        //        Dictionary<DateTime, IExpenditure> daily, Dictionary<DateTime, IExpenditure> monthly) yearlyAcc = (new(), 0, 0, 0, new(), new());

        //    (decimal total, decimal expense, decimal income) totalAcc = (0, 0, 0);
        //    DateTime minDate = orderedTransactions[0].BookingDate;
        //    DateTime maxDate = orderedTransactions[0].BookingDate;

        //    Dictionary<DateTime, IExpenditure> daily = new();
        //    Dictionary<DateTime, IExpenditure> monthly = new();
        //    Dictionary<DateTime, IExpenditure> yearly = new();

        //    void addDaily()
        //    {
        //        var month = new DateTime(day.Year, day.Month, 1);
        //        var year = new DateTime(day.Year, 1, 1);

        //        var dayExpenditure = new Expenditure(dailyAcc.transactions, IsAbsolute, true)
        //        {
        //            TotalAmount = dailyAcc.total,
        //            TotalExpense = IsAbsolute ? Math.Abs(dailyAcc.expense) : dailyAcc.expense,
        //            TotalIncome = dailyAcc.income,
        //            StartDate = day,
        //            EndDate = day.AddDays(1),
        //        };

        //        dayExpenditure._dailyExpensesLazy = new(() => new Dictionary<DateTime, IExpenditure>()
        //        {
        //            [day] = dayExpenditure
        //        });

        //        dayExpenditure._monthlyExpensesLazy = new(() => new Dictionary<DateTime, IExpenditure>()
        //        {
        //            [month] = dayExpenditure
        //        });

        //        dayExpenditure._yearlyExpensesLazy = new(() => new Dictionary<DateTime, IExpenditure>()
        //        {
        //            [year] = dayExpenditure
        //        });

        //        daily.Add(day, dayExpenditure);

        //        // acc month
        //        monthlyAcc.transactions.AddRange(dailyAcc.transactions);
        //        monthlyAcc.total += dailyAcc.total;
        //        monthlyAcc.expense += dailyAcc.expense;
        //        monthlyAcc.income += dailyAcc.income;
        //        monthlyAcc.daily.Add(day, dayExpenditure);
        //        yearlyAcc.daily.Add(day, dayExpenditure);

        //        // reset daily acc
        //        dailyAcc = (new(), 0, 0, 0);
        //    }

        //    void addMonthly()
        //    {
        //        var month = new DateTime(day.Year, day.Month, 1);
        //        var year = new DateTime(day.Year, 1, 1);

        //        var monthExpenditure = new Expenditure(monthlyAcc.transactions, IsAbsolute, true)
        //        {
        //            TotalAmount = monthlyAcc.total,
        //            TotalExpense = monthlyAcc.expense,
        //            TotalIncome = monthlyAcc.income,
        //            StartDate = month,
        //            EndDate = month.AddMonths(1),
        //        };

        //        monthExpenditure._dailyExpensesLazy = new(() => monthlyAcc.daily);

        //        monthExpenditure._monthlyExpensesLazy = new(() => new Dictionary<DateTime, IExpenditure>()
        //        {
        //            [month] = monthExpenditure
        //        });

        //        monthExpenditure._yearlyExpensesLazy = new(() => new Dictionary<DateTime, IExpenditure>()
        //        {
        //            [year] = monthExpenditure
        //        });

        //        monthly.Add(month, monthExpenditure);


        //        // acc year
        //        yearlyAcc.transactions.AddRange(monthlyAcc.transactions);
        //        yearlyAcc.total += monthlyAcc.total;
        //        yearlyAcc.expense += monthlyAcc.expense;
        //        yearlyAcc.income += monthlyAcc.income;
        //        yearlyAcc.monthly.Add(month, monthExpenditure);

        //        // reset monthly acc
        //        monthlyAcc = (new(), 0, 0, 0, new());
        //    }

        //    void addYearly()
        //    {
        //        var year = new DateTime(day.Year, 1, 1);

        //        var yearExpenditure = new Expenditure(yearlyAcc.transactions, IsAbsolute, true)
        //        {
        //            TotalAmount = yearlyAcc.total,
        //            TotalExpense = yearlyAcc.expense,
        //            TotalIncome = yearlyAcc.income,
        //            StartDate = year,
        //            EndDate = year.AddYears(1),
        //        };

        //        yearExpenditure._dailyExpensesLazy = new(() => yearlyAcc.daily);

        //        yearExpenditure._monthlyExpensesLazy = new(() => yearlyAcc.monthly);

        //        yearExpenditure._yearlyExpensesLazy = new(() => new Dictionary<DateTime, IExpenditure>()
        //        {
        //            [year] = yearExpenditure
        //        });

        //        yearly.Add(year, yearExpenditure);

        //        // acc total
        //        totalAcc.total += yearlyAcc.total;
        //        totalAcc.expense += yearlyAcc.expense;
        //        totalAcc.income += yearlyAcc.income;

        //        // reset yearly acc
        //        yearlyAcc = (new(), 0, 0, 0, new(), new());
        //    }

        //    foreach (var t in orderedTransactions)
        //    {
        //        var transactionDay = t.BookingDate.Date;

        //        if (transactionDay.Day != day.Day)
        //        {
        //            addDaily();

        //            if (transactionDay.Month != day.Month)
        //            {
        //                addMonthly();

        //                if (transactionDay.Year != day.Year)
        //                {
        //                    addYearly();
        //                }
        //            }


        //            // update date
        //            day = transactionDay;
        //        }

        //        dailyAcc.transactions.Add(t);

        //        dailyAcc.total += t.Amount;

        //        if (t.Type is TransactionType.Expense)
        //        {
        //            dailyAcc.expense += t.Amount;
        //        }
        //        else if (t.Type is TransactionType.Income)
        //        {
        //            dailyAcc.income += t.Amount;
        //        }

        //        if (minDate > t.BookingDate)
        //        {
        //            minDate = t.BookingDate;
        //        }

        //        if (maxDate < t.BookingDate)
        //        {
        //            maxDate = t.BookingDate;
        //        }
        //    }

        //    addDaily();
        //    addMonthly();
        //    addYearly();

        //    _dailyExpensesLazy = new(() => new ReadOnlyDictionary<DateTime, IExpenditure>(daily));
        //    _monthlyExpensesLazy = new(() => new ReadOnlyDictionary<DateTime, IExpenditure>(monthly));
        //    _yearlyExpensesLazy = new(() => new ReadOnlyDictionary<DateTime, IExpenditure>(yearly));

        //    return (totalAcc.total, totalAcc.expense, totalAcc.income, minDate, maxDate);
        //}

        private class ExpenditureAcc<T>
        {
            public readonly List<T> Items;
            public decimal Total;
            public decimal Income;
            public decimal Expense;

            public ExpenditureAcc()
            {
                Items = new();
                Total = 0;
                Income = 0;
                Expense = 0;
            }
        }

        private Dictionary<DateTime, IExpenditure> GroupIntoExpenditure<T>(
            IEnumerable<T> items,
            Func<T, DateTime> keySelector,
            Func<T, (decimal total, decimal income, decimal expense)> getAmounts,
            Func<IEnumerable<T>, IEnumerable<Transaction>> getTransactions,
            DateGroupScope scope,
            bool sort)
        {

            IDictionary<DateTime, ExpenditureAcc<T>> dict =
                sort ? new SortedDictionary<DateTime, ExpenditureAcc<T>>()
                     : new();

            var span = this.ExpenditurePeriod;

            foreach (var item in items)
            {
                var key = keySelector(item);
                if (!dict.TryGetValue(key, out var grouping))
                {
                    grouping = new();
                    dict.Add(key, grouping);
                }

                var (total, income, expense) = getAmounts(item);

                grouping.Total += total;
                grouping.Income += income;
                grouping.Expense += expense;

                grouping.Items.Add(item);
            }

            return dict.ToDictionary(g => g.Key, g =>
            {
                return new Expenditure(getTransactions(g.Value.Items), IsAbsolute, scope, g.Key, this)
                {
                    TotalAmount = g.Value.Total,
                    TotalIncome = g.Value.Income,
                    TotalExpense = IsAbsolute ? Math.Abs(g.Value.Expense) : g.Value.Expense,
                } as IExpenditure;
            });
        }

        private Dictionary<DateTime, IExpenditure> GroupTransactions(
            DateGroupScope scope, bool sort = true)
        {
            Func<Transaction, DateTime> keySelector = scope switch
            {
                DateGroupScope.Daily => (Transaction t) => t.BookingDate.Date,
                DateGroupScope.Monthly => (Transaction t) => new DateTime
                (
                    t.BookingDate.Year,
                    t.BookingDate.Month,
                    1
                ),
                DateGroupScope.Yearly => (Transaction t) => new DateTime
                (
                    t.BookingDate.Year,
                    1,
                    1
                ),
                _ => (Transaction t) => t.BookingDate
            };

            //return Transactions.GroupBy(keySelector).ToDictionary(g => g.Key, g =>
            //{
            //    return new Expenditure(g, IsAbsolute, scope, g.Key, this)
            //    {
            //        TotalAmount = g.Sum(x => x.Amount),
            //        TotalIncome = g.Sum(x => Math.Max(0, x.Amount)),
            //        TotalExpense = g.Sum(x => Math.Min(0, x.Amount))
            //    } as IExpenditure;
            //});

            return GroupIntoExpenditure(
                Transactions,
                keySelector,
                getAmounts: t => (t.Amount, Math.Max(0, t.Amount), Math.Min(0, t.Amount)),
                transactions => transactions,
                scope,
                sort);
        }

        private Dictionary<DateTime, IExpenditure> GroupExpenditures(
            //IEnumerable<IExpenditure> expenditures,
            DateGroupScope scope)
        {
            Func<IExpenditure, DateTime> keySelector = scope switch
            {
                DateGroupScope.Daily => e => e.StartDate.Date,
                DateGroupScope.Monthly => e => new DateTime
                (
                    e.StartDate.Year,
                    e.StartDate.Month,
                    1
                ),
                DateGroupScope.Yearly => e => new DateTime
                (
                    e.StartDate.Year,
                    1,
                    1
                ),
                _ => e => e.StartDate
            };

            IEnumerable<IExpenditure> expenditures = scope switch
            {
                DateGroupScope.Monthly => DailyExpenses.Values,
                DateGroupScope.Yearly => MonthlyExpenses.Values,
                _ => YearlyExpenses.Values
            };

            return GroupIntoExpenditure(
                expenditures,
                keySelector,
                getAmounts: ex => (ex.TotalAmount, ex.TotalIncome, ex.TotalExpense),
                getTransactions: expenditures => expenditures.SelectMany(e => e.Transactions),
                scope,
                false);
        }

        private Expenditure(IEnumerable<Transaction> transactions, bool absolute, DateGroupScope scope, DateTime startDate, IExpenditure baseExpenditure)
            : base(transactions, absolute)
        {
            if (scope == DateGroupScope.Daily)
            {
                StartDate = startDate;
                EndDate = startDate.AddDays(1);

                DailyExpenses = new Dictionary<DateTime, IExpenditure>(1)
                {
                    [startDate] = this
                };

                MonthlyExpenses = new Dictionary<DateTime, IExpenditure>(1)
                {
                    [new(startDate.Year, startDate.Month, 1)] = this
                };

                YearlyExpenses = new Dictionary<DateTime, IExpenditure>(1)
                {
                    [new(startDate.Year, 1, 1)] = this
                };
            }
            else if (scope == DateGroupScope.Monthly)
            {
                StartDate = startDate;
                EndDate = startDate.AddMonths(1);

                var dailyExpenses = new Dictionary<DateTime, IExpenditure>(31);

                foreach (var kvp in baseExpenditure.DailyExpenses
                    .Where(kvp =>
                        kvp.Key.Year == startDate.Year &&
                        kvp.Key.Month == startDate.Month))
                {
                    dailyExpenses.Add(kvp.Key, kvp.Value);
                }

                DailyExpenses = dailyExpenses;


                MonthlyExpenses = new Dictionary<DateTime, IExpenditure>(1)
                {
                    [startDate] = this
                };

                YearlyExpenses = new Dictionary<DateTime, IExpenditure>(1)
                {
                    [new(startDate.Year, 1, 1)] = this
                };
            }
            else if (scope == DateGroupScope.Yearly)
            {
                StartDate = startDate;
                EndDate = startDate.AddYears(1);

                var dailyExpenses = new Dictionary<DateTime, IExpenditure>(365);

                foreach (var kvp in baseExpenditure.DailyExpenses
                    .Where(kvp =>
                        kvp.Key.Year == startDate.Year &&
                        kvp.Key.Month == startDate.Month))
                {
                    dailyExpenses.Add(kvp.Key, kvp.Value);
                }

                DailyExpenses = dailyExpenses;


                var monthlyExpenses = new Dictionary<DateTime, IExpenditure>(12);

                foreach (var kvp in baseExpenditure.MonthlyExpenses
                    .Where(kvp => kvp.Key.Year == startDate.Year))
                {
                    monthlyExpenses.Add(kvp.Key, kvp.Value);
                }

                MonthlyExpenses = monthlyExpenses;
                
                YearlyExpenses = new Dictionary<DateTime, IExpenditure>(1)
                {
                    [new(startDate.Year, 1, 1)] = this
                };
            }
            else
            {
                StartDate = baseExpenditure.StartDate;
                EndDate = baseExpenditure.EndDate;
                DailyExpenses = baseExpenditure.DailyExpenses;
                MonthlyExpenses = baseExpenditure.MonthlyExpenses;
                YearlyExpenses = baseExpenditure.YearlyExpenses;
            }
        }

        public Expenditure(IEnumerable<Transaction> transactions, bool absolute, DateTime? startDate, DateTime? endDate) : base(transactions, absolute)
        {
            if (!transactions.Any())
            {
                DailyExpenses = EmptyDictionary;
                MonthlyExpenses = EmptyDictionary;
                YearlyExpenses = EmptyDictionary;
                return;
            }

            if (startDate.HasValue)
            {
                StartDate = startDate.Value;
            }
            if (endDate.HasValue)
            {
                EndDate = endDate.Value;
            }

            //var groupedByDay = Transactions.GroupBy(t => t.BookingDate.Date);

            DailyExpenses = GroupTransactions(DateGroupScope.Daily, sort: false);
            //DailyExpenses = Transactions.GroupBy(t => t.BookingDate.Date)
            //    .ToDictionary(g => g.Key, g =>

            //        new Expenditure(g, IsAbsolute, DateGroupScope.Daily, g.Key, this)
            //        {
            //            TotalAmount = g.Sum(x => x.Amount),
            //            TotalIncome = g.Sum(x => Math.Max(0, x.Amount)),
            //            TotalExpense = absolute ? Math.Abs(g.Sum(x => Math.Min(0, x.Amount))) : g.Sum(x => Math.Min(0, x.Amount)),
            //        } as IExpenditure
            //    );

            //DailyExpenses = groupedByDay.ToDictionary(g => g.Key, g =>
            //{
            //    var (total, income, expense) = CalculateSum(g, IsAbsolute);

            //    //var dailyExpenses = new Dictionary<DateTime, IExpenditure>();
            //    //var monthlyExpenses = new Dictionary<DateTime, IExpenditure>();
            //    //var yearlyExpenses = new Dictionary<DateTime, IExpenditure>();

            //    var expenditure = new Expenditure(g, IsAbsolute, DateGroupScope.Daily, g.Key, this)
            //    {
            //        TotalAmount = total,
            //        TotalIncome = income,
            //        TotalExpense = expense,
            //        //StartDate = g.Key,
            //        //EndDate = g.Key.AddDays(1),
            //        //DailyExpenses = dailyExpenses,
            //        //MonthlyExpenses = monthlyExpenses,
            //        //YearlyExpenses = yearlyExpenses
            //    };

            //    //dailyExpenses.Add(g.Key, expenditure);
            //    //monthlyExpenses.Add(new(g.Key.Year, g.Key.Month, 1), expenditure);
            //    //yearlyExpenses.Add(new(g.Key.Year, 1, 1), expenditure);

            //    return expenditure as IExpenditure;
            //});


            MonthlyExpenses = GroupExpenditures(DateGroupScope.Monthly);
            //MonthlyExpenses = Transactions.GroupBy(t => new DateTime(t.BookingDate.Year, t.BookingDate.Month, 1))
            //    .ToDictionary(g => g.Key, g =>
            //        new Expenditure(g, IsAbsolute, DateGroupScope.Monthly, g.Key, this)
            //        {
            //            TotalAmount = g.Sum(x => x.Amount),
            //            TotalIncome = g.Sum(x => Math.Max(0, x.Amount)),
            //            TotalExpense = g.Sum(x => Math.Min(0, x.Amount)),
            //        } as IExpenditure
            //    );

            //MonthlyExpenses = groupedByMonth.ToDictionary(g => g.Key, g =>
            //{
            //    var dailyExpenses = DailyExpenses
            //        .Where(kvp =>
            //            kvp.Key.Year == g.Key.Year &&
            //            kvp.Key.Month == g.Key.Month)
            //        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            //    var monthlyExpenses = new Dictionary<DateTime, IExpenditure>();
            //    var yearlyExpenses = new Dictionary<DateTime, IExpenditure>();

            //    var expenditure = new Expenditure(g.Value.transactions, IsAbsolute, false)
            //    {
            //        TotalAmount = g.Value.total,
            //        TotalIncome = g.Value.income,
            //        TotalExpense = g.Value.expense,
            //        StartDate = g.Key,
            //        EndDate = g.Key.AddMonths(1),
            //        DailyExpenses = dailyExpenses,
            //        MonthlyExpenses = monthlyExpenses,
            //        YearlyExpenses = yearlyExpenses
            //    };

            //    monthlyExpenses.Add(g.Key, expenditure);
            //    yearlyExpenses.Add(new(g.Key.Year, 1, 1), expenditure);

            //    return expenditure as IExpenditure;
            //});


            YearlyExpenses = GroupExpenditures(DateGroupScope.Yearly);
            //YearlyExpenses = Transactions.GroupBy(t => new DateTime(t.BookingDate.Year, 1, 1))
            //    .ToDictionary(g => g.Key, g =>
            //        new Expenditure(g, IsAbsolute, DateGroupScope.Yearly, g.Key, this)
            //        {
            //            TotalAmount = g.Sum(x => x.Amount),
            //            TotalIncome = g.Sum(x => Math.Max(0, x.Amount)),
            //            TotalExpense = g.Sum(x => Math.Min(0, x.Amount)),
            //        } as IExpenditure
            //    );

            //(StartDate, EndDate) = Transactions.Select(t => t.BookingDate).MinAndMax();

            // Take advantage of the sorted dictionary (DailyExpenses)
            StartDate = startDate ?? DailyExpenses.Values.First().Transactions.Min(t => t.BookingDate);
            EndDate = endDate ?? DailyExpenses.Values.Last().Transactions.Max(t => t.BookingDate);

            (TotalAmount, TotalIncome, TotalExpense) =
                YearlyExpenses.Values.Aggregate((total: 0m, income: 0m, expense: 0m),
                    (sum, expenditure) =>
                        (
                         sum.total + expenditure.TotalAmount,
                         sum.income + expenditure.TotalIncome,
                         sum.expense + expenditure.TotalExpense
                        )
                    );
        }

        #region Grouping

        private IReadOnlyDictionary<DateTime, IExpenditure> GroupThis(DateGroupScope scope)
        {
            var firstDate = Transactions[0].BookingDate;

            return new Dictionary<DateTime, IExpenditure>()
            {
                [GetScopedDate(firstDate, scope)] = this
            };
        }

        private static DateTime GetScopedDate(DateTime date, DateGroupScope scope)
        {
            return scope switch
            {
                DateGroupScope.Daily => date.Date,
                DateGroupScope.Monthly => new DateTime
                (
                    date.Year,
                    date.Month,
                    1
                ),
                DateGroupScope.Yearly => new DateTime
                (
                    date.Year,
                    1,
                    1
                ),
                _ => date
            };
        }

        //private IReadOnlyDictionary<DateTime, IExpenditure> GroupTransactions(DateGroupScope scope)
        //{
        //    //Func<Transaction, DateTime> dateSelector = scope switch
        //    //{
        //    //    "daily" => (Transaction t) => t.BookingDate.Date,
        //    //    "monthly" => (Transaction t) => new DateTime
        //    //    (
        //    //        t.BookingDate.Year,
        //    //        t.BookingDate.Month,
        //    //        1
        //    //    ),
        //    //    "yearly" => (Transaction t) => new DateTime
        //    //    (
        //    //        t.BookingDate.Year,
        //    //        1,
        //    //        1
        //    //    ),
        //    //    _ => (Transaction t) => t.BookingDate
        //    //};

        //    //Func<DateTime, DateTime> addOne = scope switch
        //    //{
        //    //    DateGroupScope.Daily => (DateTime d) => d.AddDays(1),
        //    //    DateGroupScope.Monthly => (DateTime d) => d.AddMonths(1),
        //    //    DateGroupScope.Yearly => (DateTime d) => d.AddYears(1),
        //    //    _ => (DateTime d) => d
        //    //};

        //    //var grouped = Transactions.GroupBy(GetScopedDate();

        //    //return grouped.ToDictionary(g => g.Key, g =>
        //    //{
        //    //    var (total, income, expense) = CalculateSum(g, IsAbsolute);

        //    //    return new Expenditure(g, IsAbsolute, scope)
        //    //    {
        //    //        TotalAmount = total,
        //    //        TotalIncome = income,
        //    //        TotalExpense = expense,
        //    //        StartDate = g.Key,
        //    //        EndDate = addOne(g.Key)
        //    //    } as IExpenditure;
        //    //});
        //    switch (scope)
        //    {
        //        case DateGroupScope.Daily:
        //            {
        //                var groupedByDay = Transactions.GroupBy(t => t.BookingDate.Date);

        //                return groupedByDay.ToDictionary(g => g.Key, g =>
        //                {
        //                    var (total, income, expense) = CalculateSum(g, IsAbsolute);

        //                    return new Expenditure(g, IsAbsolute, scope)
        //                    {
        //                        TotalAmount = total,
        //                        TotalIncome = income,
        //                        TotalExpense = expense,
        //                        StartDate = g.Key,
        //                        EndDate = g.Key.AddDays(1)
        //                    } as IExpenditure;
        //                });
        //            }
        //        case DateGroupScope.Monthly:
        //            {
        //                var groupedByMonth = Transactions.GroupBy(t => new DateTime
        //                (
        //                    t.BookingDate.Year,
        //                    t.BookingDate.Month,
        //                    1
        //                ));

        //                return groupedByMonth.ToDictionary(g => g.Key, g =>
        //                {
        //                    var (total, income, expense) = CalculateSum(g, IsAbsolute);

        //                    return new Expenditure(g, IsAbsolute, scope)
        //                    {
        //                        TotalAmount = total,
        //                        TotalIncome = income,
        //                        TotalExpense = expense,
        //                        StartDate = g.Key,
        //                        EndDate = g.Key.AddMonths(1)
        //                    } as IExpenditure;
        //                });
        //            }
        //        case DateGroupScope.Yearly:
        //            {
        //                var groupedByYear = Transactions.GroupBy(t => new DateTime
        //                (
        //                    t.BookingDate.Year,
        //                    1,
        //                    1
        //                ));

        //                return groupedByYear.ToDictionary(g => g.Key, g =>
        //                {
        //                    var (total, income, expense) = CalculateSum(g, IsAbsolute);

        //                    return new Expenditure(g, IsAbsolute, scope)
        //                    {
        //                        TotalAmount = total,
        //                        TotalIncome = income,
        //                        TotalExpense = expense,
        //                        StartDate = g.Key,
        //                        EndDate = g.Key.AddYears(1)
        //                    } as IExpenditure;
        //                });
        //            }
        //        default: throw new ArgumentException("Unknown scope", nameof(scope));
        //    }
        //}

        private enum DateGroupScope
        {
            Daily = 0,
            Monthly = 1,
            Yearly = 2,
            All = 3
        }

        #endregion

        private static (decimal total, decimal income, decimal expense) AggregateExpenditures(IEnumerable<IExpenditure> expenditures, bool absolute)
        {
            (decimal total, decimal income, decimal expense) result = (0, 0, 0);

            foreach (var e in expenditures)
            {
                result.total += e.TotalAmount;
                result.expense += e.TotalExpense;
                result.income += e.TotalIncome;
            }

            if (absolute)
            {
                return (result.total, Math.Abs(result.income), Math.Abs(result.expense));
            }

            return result;
        }

        private static (decimal total, decimal income, decimal expense) CalculateSum(IEnumerable<Transaction> transactions, bool absolute)
        {
            (decimal total, decimal income, decimal expense) result = (0, 0, 0);

            foreach (var t in transactions)
            {
                result.total += t.Amount;

                if (t.Type is TransactionType.Expense)
                {
                    result.expense += t.Amount;
                }
                else if (t.Type is TransactionType.Income)
                {
                    result.income += t.Amount;
                }
            }

            if (absolute)
            {
                return (result.total, result.income, Math.Abs(result.expense));
            }

            return result;
        }

        public IExpenditure SubRange(DateTime? startDate = null, DateTime? endDate = null)
        {
            if (startDate < StartDate)
            {
                throw new ArgumentException("New start date must be greater than the current start date.", nameof(startDate));
            }

            if (endDate > EndDate)
            {
                throw new ArgumentException("New end date must be smaller than the current end date.", nameof(endDate));
            }

            var transactions = Transactions.Where(t => t.BookingDate >= startDate && t.BookingDate <= endDate);

            return new Expenditure(transactions, IsAbsolute, startDate ?? StartDate, endDate ?? EndDate)
            {
                StartDate = startDate ?? StartDate,
                EndDate = endDate ?? EndDate
            };
        }

        public decimal GetMonthlyAverage(TransactionType type = TransactionType.None, bool includeEmptyMonths = true, bool limitToCurrentMonth = true)
        {
            if (Transactions.Count == 0)
            {
                return 0;
            }

            int totalMonths = MonthlyExpenses.Count;

            if (includeEmptyMonths)
            {
                totalMonths = MonthDifference(EndDate, StartDate, limitToCurrentMonth) + 1;
            }

            return GetAmountByType(type) / totalMonths;
        }

        public decimal GetDailyAverage(TransactionType type = TransactionType.None, bool includeEmptyDays = true, bool limitToCurrentDate = true)
        {
            if (Transactions.Count == 0)
            {
                return 0;
            }

            int totalDays = DailyExpenses.Count;

            if (includeEmptyDays)
            {
                totalDays = (int)((limitToCurrentDate ? LimitToCurrentDate(EndDate.Date) : EndDate.Date) -
                    (limitToCurrentDate ? LimitToCurrentDate(StartDate.Date) : StartDate.Date)).TotalDays;
            }

            return GetAmountByType(type) / totalDays;
        }

        public int GetMonths(bool limitToCurrentMonth = true)
        {
            return MonthDifference(EndDate, StartDate, limitToCurrentMonth) + 1;
        }

        private static int MonthDifference(DateTime lValue, DateTime rValue, bool limitToCurrentMonth = false)
        {
            if (limitToCurrentMonth)
            {
                lValue = new(Math.Min(DateTime.Now.Ticks, lValue.Ticks));
                rValue = new(Math.Min(DateTime.Now.Ticks, rValue.Ticks));
            }

            return Math.Abs((lValue.Month - rValue.Month) + 12 * (lValue.Year - rValue.Year));
        }

        private static DateTime LimitToCurrentDate(DateTime date)
        {
            return new(Math.Min(DateTime.Now.Ticks, date.Ticks));
        }
    }
}
