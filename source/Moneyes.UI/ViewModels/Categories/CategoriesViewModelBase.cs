using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    internal abstract class CategoriesViewModelBase<TCategoryViewModel> : ViewModelBase
        where TCategoryViewModel : CategoryViewModel
    {
        private ObservableCollection<TCategoryViewModel> _categories = new();
        public ObservableCollection<TCategoryViewModel> Categories
        {
            get
            {
                return _categories;
            }
            set
            {
                _categories = value;
                OnPropertyChanged();
            }
        }

        private EditCategoryViewModel _editCategoryViewModel;
        public EditCategoryViewModel EditCategoryViewModel
        {
            get
            {
                return _editCategoryViewModel;
            }
            set
            {
                _editCategoryViewModel = value;
                OnPropertyChanged();
            }
        }

        private EditCategoryViewModel _addCategoryViewModel;
        public EditCategoryViewModel AddCategoryViewModel
        {
            get
            {
                return _addCategoryViewModel;
            }
            set
            {
                _addCategoryViewModel = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddCommand { get; protected set; }

        public CategoriesViewModelBase(CategoryViewModelFactory factory)
        {
            AddCommand = new AsyncCommand(async ct =>
            {
                AddCategoryViewModel = factory.CreateAddCategoryViewModel();
            });
        }
    }
}
