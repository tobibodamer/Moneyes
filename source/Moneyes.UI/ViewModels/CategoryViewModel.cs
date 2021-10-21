using Moneyes.Core;

namespace Moneyes.UI.ViewModels
{
    internal class CategoryViewModel : ViewModelBase
    {
        private readonly Category _category;
        private string _displayName;
        private string _name;
        private string _totalExpense;
        private string _target;
        internal Category Category => _category;
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
                Name = "[No category]";
                _displayName = $"[No category]: {totalExpense} €";
            }
            else
            {
                Name = category.Name;
                _displayName = $"{category.Name}: {totalExpense} / {category.Target} €";
            }
        }

        public CategoryViewModel(string name, decimal totalExpense, decimal target = 0)
        {
            Name = name;
            TotalExpense = $"{(int)totalExpense}";
            Target = $"{(int)target}";
            _displayName = $"{name}: {totalExpense} / {target} €";
        }

        public override string ToString()
        {
            return _displayName;
        }
    }
}
