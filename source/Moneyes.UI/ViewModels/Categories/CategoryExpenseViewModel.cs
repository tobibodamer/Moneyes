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

        /// <summary>
        /// Total summed up amount over the expenditure period.
        /// </summary>
        public decimal TotalAmount { get; init; }

        /// <summary>
        /// Absolute total summed up amount over the expenditure period
        /// </summary>
        public decimal AbsoluteTotalAmount => Math.Abs(TotalAmount);

        /// <summary>
        /// Average amount over the expenditure period.
        /// </summary>
        public decimal AverageAmount { get; }

        /// <summary>
        /// Gets whether the total amount is over the target, or false when no target is set.
        /// </summary>
        public bool IsOver { get; }

        /// <summary>
        /// Gets whether the toal amount is over the target extrapolated to the monthly range.
        /// </summary>
        public bool IsOverExtrapolated { get; }

        public decimal? Difference { get; }

        /// <summary>
        /// Target extrapolated to the monthly range.
        /// </summary>
        public decimal? TargetExtrapolated { get; }

        public bool IsExpenseCategory => Category.IsExpenseCategory;

        /// <summary>
        /// Gets the current amount towards the target.
        /// </summary>
        public decimal AmountOfTarget { get; }


        /// <summary>
        /// Gets the current monthly average amount towards the target.
        /// </summary>
        public decimal AverageAmountOfTarget { get; }

        /// <summary>
        /// Gets the total expense amount.
        /// </summary>
        public decimal TotalExpense { get; }

        /// <summary>
        /// Gets the total income amount.
        /// </summary>
        public decimal TotalIncome { get; }

        public TransactionType ExpenditureType { get; }

        public CategoryExpenseViewModel(Category category, IExpenditure expenditure, TransactionType expenditureType,
            ICategoryService categoryService, ITransactionService transactionService,
            IStatusMessageService statusMessageService)
            : base(categoryService, transactionService, statusMessageService)
        {
            Category = category;
            ExpenditureType = expenditureType;
            TotalAmount = expenditure.TotalAmount;
            AverageAmount = expenditure.GetMonthlyAverage();

            TargetExtrapolated = Target.HasValue
                ? Target.Value * expenditure.GetMonths(limitToCurrentMonth: true)
                : null;

            AmountOfTarget = AbsoluteTotalAmount;
            AverageAmountOfTarget = Math.Abs(AverageAmount);

            TotalExpense = expenditure.IsAbsolute ? expenditure.TotalAmount : Math.Max(0, TotalAmount * -1);
            TotalIncome = expenditure.IsAbsolute ? expenditure.TotalAmount : Math.Max(0, TotalAmount);
            
            if (IsExpenseCategory && Target.HasValue)
            {
                AmountOfTarget = TotalExpense;
                AverageAmountOfTarget = Math.Max(0, AverageAmount * -1);
                IsOver = AmountOfTarget > Target;
                IsOverExtrapolated = AmountOfTarget > TargetExtrapolated;
                Difference = Math.Abs(Target.Value - AbsoluteTotalAmount);
            }
            else if (Target.HasValue)
            {
                AmountOfTarget = TotalIncome;
                AverageAmountOfTarget = Math.Max(0, AverageAmount);
                IsOver = AmountOfTarget > Target;
                IsOverExtrapolated = AmountOfTarget > TargetExtrapolated;
                Difference = Math.Abs(Target.Value - TotalAmount);
            }

            if (IsNoCategory)
            {
                DisplayName = $"-- No category -- ({AmountOfTarget} €)";
            }
            else if (Target.HasValue)
            {

                DisplayName = $"{category.Name} ({AmountOfTarget} / {category.Target} €)";
            }
            else
            {
                DisplayName = $"{category.Name} ({AmountOfTarget} €)";
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
