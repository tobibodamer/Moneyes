using Moneyes.Core;
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

        public ObservableCollection<CategoryExpenseViewModel> SubCatgeories { get; set; } = new();

        public ICommand AssignToTransaction { get; set; }

        public bool IsOver => Target > 0 && TotalExpense > Target;

        public decimal? Difference => Target == null ? null : Math.Abs(Target.Value - TotalExpense);

        public CategoryExpenseViewModel(Category category, decimal totalExpense)
        {
            Category = category;
            TotalExpense = totalExpense;

            if (IsNoCategory)
            {
                DisplayName = $"-- No category -- ({totalExpense} €)";
            }
            else if (category.Target > 0)
            {

                DisplayName = $"{category.Name} ({totalExpense} / {category.Target} €)";
            }
            else
            {
                DisplayName = $"{category.Name} ({totalExpense} €)";
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
