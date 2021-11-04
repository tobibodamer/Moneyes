using Moneyes.Core;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    /// <summary>
    /// A simple view model for a <see cref="Core.Category"/>.
    /// </summary>
    internal class CategoryViewModel : ViewModelBase
    {
        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        private decimal? _target;
        public decimal? Target
        {
            get => _target;
            set
            {
                _target = value;
                OnPropertyChanged();
            }
        }

        private Category _parent;
        public Category Parent
        {
            get => _parent;
            set
            {
                _parent = (value == Category.NoCategory) ? null : value;

                OnPropertyChanged();
            }
        }

        private Category _category;

        public virtual Category Category
        {
            get => _category;
            set
            {
                _category = value;

                Name = _category.Name;
                Target = _category.Target;

                if (_category.Parent is not null)
                {
                    Parent = _category.Parent;
                }
            }
        }

        public ICommand EditCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public bool IsNoCategory => _category == Category.NoCategory;

        public bool IsRealCategory => !_category.Equals(Category.NoCategory)
            && !_category.Equals(Category.AllCategory);

        public CategoryViewModel()
        {
        }
    }
}
