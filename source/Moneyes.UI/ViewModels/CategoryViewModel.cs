using Moneyes.Core;
using System.Collections.Generic;

namespace Moneyes.UI.ViewModels
{
    internal class CategoryViewModel : ViewModelBase
    {
        private readonly Category _category;
        private string _name;
        private string _totalExpense;
        private string _target;
        internal Category Category => _category;

        public List<CategoryViewModel> SubCatgeories { get; set; } = new();

        public bool IsNoCategory { get; set; }
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string TotalExpense
        {
            get => _totalExpense;
            set
            {
                _totalExpense = value;
                OnPropertyChanged(nameof(TotalExpense));
            }
        }

        public string Target
        {
            get => _target;
            set
            {
                _target = value;
                OnPropertyChanged(nameof(Target));
            }
        }

        public CategoryViewModel(Category category, decimal totalExpense)
        {
            _category = category;

            TotalExpense = $"{(int)totalExpense}";
            Target = $"{(int)category.Target}";

            if (category == Category.NoCategory)
            {
                IsNoCategory = true;
                OnPropertyChanged(nameof(IsNoCategory));
                Name = "[No category]";
                DisplayName = $"-- No category --  ({totalExpense} €)";
            }
            else
            {
                Name = category.Name;

                if (category.Target > 0)
                {
                    DisplayName = $"{category.Name}  ({totalExpense} / {category.Target} €)";
                }
                else
                {
                    DisplayName = $"{category.Name}  ({totalExpense}€)";
                }
            }
        }

        public CategoryViewModel(string name, decimal totalExpense, decimal target = 0)
        {
            Name = name;
            TotalExpense = $"{(int)totalExpense}";
            Target = $"{(int)target}";

            if (target > 0)
            {
                DisplayName = $"{name}  ({totalExpense} / {target} €)";
            }
            else
            {
                DisplayName = $"{name}  ({totalExpense}€)";
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
