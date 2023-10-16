using LiteDB;
using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using Moneyes.LiveData;
using Moneyes.UI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Moneyes.UI.ViewModels
{
    internal class ExpenseCategoriesViewModel : CategoriesViewModelBase<CategoryExpenseViewModel>
    {
        private readonly IExpenseIncomeService _expenseIncomeService;

        public ExpenseCategoriesViewModel(CategoryViewModelFactory factory,
            ICategoryService categoryService,
            IExpenseIncomeService expenseIncomeService,
            IStatusMessageService statusMessageService)
            : base(factory, categoryService, statusMessageService)
        {
            _expenseIncomeService = expenseIncomeService;
        }

        protected override Task<List<CategoryExpenseViewModel>> GetCategoriesAsync(TransactionFilter filter, CategoryTypes categoryTypes, bool flat)
        {
            return Task.Run(() =>
            {
                List<CategoryExpenseViewModel> categoryViewModels = new();

                var expenditureResult = _expenseIncomeService.GetExpenditurePerCategory(filter, categoryTypes, includeSubCategories: true);

                if (!expenditureResult.IsSuccessful)
                {
                    return categoryViewModels;
                    // Failed to get expenses
                }

                foreach ((Category category, IExpenditure expenses) in expenditureResult.Data)
                {
                    categoryViewModels.Add(
                        Factory.CreateCategoryExpenseViewModel(category, expenses, filter.TransactionType,
                            editViewModel =>
                            {
                                EditCategoryViewModel = editViewModel;
                            }));
                }

                if (!flat)
                {
                    // Set sub categories
                    SetSubCategories(categoryViewModels);
                }

                return categoryViewModels;
            });
        }

        protected override IComparer<CategoryExpenseViewModel> GetComparer(bool order)
        {
            if (order)
            {
                return new ExpenseComparer();
            }

            return base.GetComparer(order);
        }

        class ExpenseComparer : IComparer<CategoryExpenseViewModel>
        {
            public int Compare(CategoryExpenseViewModel x, CategoryExpenseViewModel y)
            {
                if (x.Target > 0 && (y.Target == 0 || y.Target == null))
                {
                    return -1;
                }
                else if ((x.Target == 0 || x.Target == null) && y.Target > 0)
                {
                    return 1;
                }

                return x.TotalAmount.CompareTo(y.TotalAmount) * -1;
            }
        }
    }
}
