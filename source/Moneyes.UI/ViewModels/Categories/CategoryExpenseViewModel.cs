using Moneyes.Core;
using Moneyes.UI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    internal class CategoryExpenseViewModel : CategoryViewModel
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        private decimal _totalExpense;
        public decimal TotalExpense
        {
            get => _totalExpense;
            set
            {
                _totalExpense = value;
                OnPropertyChanged();
            }
        }

        public decimal AverageExpense { get; }

        public bool IsOver => Target > 0 && TotalExpense > Target;

        public bool IsOverExtrapolated => TargetExtrapolated > 0 && TotalExpense > TargetExtrapolated;

        public decimal? Difference => Target == null ? null : Math.Abs(Target.Value - TotalExpense);

        public decimal? TargetExtrapolated { get; }

        public CategoryExpenseViewModel(Category category, Expenses expenses,
            ICategoryService categoryService, IStatusMessageService statusMessageService)
            : base(categoryService, statusMessageService)
        {
            Category = category;
            TotalExpense = expenses.TotalAmount;
            AverageExpense = expenses.GetMonthlyAverage();

            TargetExtrapolated = Target.HasValue
                ? Target.Value * (Expenses.MonthDifference(expenses.StartDate, expenses.EndDate, limitToCurrentMonth: true) + 1)
                : null;

            if (IsNoCategory)
            {
                DisplayName = $"-- No category -- ({TotalExpense} €)";
            }
            else if (category.Target > 0)
            {

                DisplayName = $"{category.Name} ({TotalExpense} / {category.Target} €)";
            }
            else
            {
                DisplayName = $"{category.Name} ({TotalExpense} €)";
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
