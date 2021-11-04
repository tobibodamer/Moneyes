using Moneyes.Core;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    class CategoriesViewModel : ViewModelBase
    {
        private ObservableCollection<CategoryViewModel> _categories;
        public ObservableCollection<CategoryViewModel> Categories
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

        EditCategoryViewModel _editCategoryViewModel;
        EditCategoryViewModel _addCategoryViewModel;
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
        public ICommand AddCommand { get; }

        CategoryViewModelFactory _factory;
        protected ICategoryService CategoryService { get; }
        protected ITransactionService TransactionService { get; }
        public CategoriesViewModel(CategoryViewModelFactory factory, ICategoryService categoryService)
        {
            _factory = factory;
            CategoryService = categoryService;

            AddCommand = new AsyncCommand(async ct =>
            {
                AddCategoryViewModel = _factory.CreateAddCategoryViewModel();
            });
        }
        public virtual void UpdateCategories()
        {
            foreach (Category category in CategoryService.GetCategories().Data)
            {
                Categories.Add(
                    _factory.CreateCategoryViewModel(category, editViewModel =>
                    {
                        EditCategoryViewModel = editViewModel;
                    }));
            }
        }
    }
}
