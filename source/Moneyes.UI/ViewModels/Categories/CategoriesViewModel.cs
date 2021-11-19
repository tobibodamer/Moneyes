using Moneyes.Core;
using Moneyes.Data;

namespace Moneyes.UI.ViewModels
{
    class CategoriesViewModel : CategoriesViewModelBase<CategoryViewModel>
    {
        CategoryViewModelFactory _factory;
        protected ICategoryService CategoryService { get; }
        protected TransactionRepository TransactionService { get; }
        public CategoriesViewModel(CategoryViewModelFactory factory,
            ICategoryService categoryService, TransactionRepository transactionRepository) 
            : base(factory)
        {
            _factory = factory;
            CategoryService = categoryService;
            TransactionService = transactionRepository;
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
