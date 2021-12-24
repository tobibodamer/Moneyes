﻿using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using Moneyes.UI.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    internal abstract class CategoriesViewModelBase<TCategoryViewModel> : ViewModelBase
        where TCategoryViewModel : CategoryViewModel
    {
        protected ICategoryService CategoryService { get; }
        protected IStatusMessageService StatusMessageService { get; }
        protected CategoryViewModelFactory Factory { get; }

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

        private TCategoryViewModel _selectedCategory;
        public TCategoryViewModel SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory == value)
                {
                    return;
                }

                _selectedCategory = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddCommand { get; protected set; }

        public CategoriesViewModelBase(CategoryViewModelFactory factory, ICategoryService categoryService,
            IStatusMessageService statusMessageService)
        {
            AddCommand = new RelayCommand(() =>
            {
                AddCategoryViewModel = factory.CreateAddCategoryViewModel();
            });

            CategoryService = categoryService;
            StatusMessageService = statusMessageService;
            Factory = factory;
        }

        /// <summary>
        /// Set the sub categores of the catgeory view models, provided as a flat list.
        /// </summary>
        /// <param name="flatCategories"></param>
        protected virtual void SetSubCategories(List<TCategoryViewModel> flatCategories)
        {
            // Select all categories with a matching parent
            var categoriesWithParent = flatCategories.Where(
                c => c.Parent is not null &&
                flatCategories.Select(c => c.Category).Contains(c.Category)).ToList();

            // Add categories with parent to sub categories of parent
            foreach (var category in categoriesWithParent)
            {
                var parent = category.Parent;
                var parentCategoryViewModel = flatCategories.FirstOrDefault(c => c.Category.Idquals(parent));

                parentCategoryViewModel.SubCatgeories.Add(category);
            }

            // Remove all categories with a parent (from top level)
            flatCategories.RemoveAll(c => categoriesWithParent.Contains(c));
        }

        protected virtual Task<List<TCategoryViewModel>> GetCategoriesAsync(
           TransactionFilter filter, CategoryTypes categoryTypes, bool flat)
        {
            return Task.Run(() =>
            {
                List<TCategoryViewModel> categoryViewModels = new();

                IEnumerable<Category> categories = CategoryService.GetCategories(categoryTypes);

                foreach (Category category in categories)
                {
                    categoryViewModels.Add(CreateEntry(category, filter, categoryTypes, flat));
                }

                if (!flat)
                {
                    // Set sub categories
                    SetSubCategories(categoryViewModels);
                }

                return categoryViewModels;
            });
        }

        /// <summary>
        /// Create a view model entry from a <see cref="Category"/>.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        protected abstract TCategoryViewModel CreateEntry(
            Category category, TransactionFilter filter, CategoryTypes categoryTypes, bool flat);

        /// <summary>
        /// Dynamically updates the list of category viewmodels by inserting new and updating existing entries.
        /// </summary>
        /// <param name="categories"></param>
        protected virtual void UpdateCategoriesInternal(IList<TCategoryViewModel> categories,
            IComparer<TCategoryViewModel> comparer)
        {
            var previouslySelectedCategory = SelectedCategory?.Category;

            Categories.DynamicUpdate(
                categories,
                (c1, c2) => c1.Category.Idquals(c2.Category),
                comparer);

            if (previouslySelectedCategory != null)
            {
                SelectCategory(previouslySelectedCategory);
            }

            if (SelectedCategory is null)
            {
                SelectCategory(Category.AllCategory);
            }

            OnPropertyChanged(nameof(Categories));
        }

        /// <summary>
        /// Select the given category using the default ID selector.
        /// </summary>
        /// <param name="category"></param>
        public void SelectCategory(Category category)
        {
            var categoryExpense = Categories
                .FirstOrDefault(c => c.Category != null && c.Category == category);

            SelectedCategory = categoryExpense;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the given category is selected.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public bool IsSelected(Category category)
        {
            return SelectedCategory.Category == category;
        }

        /// <summary>
        /// Updates the categories by reloading them using the given <paramref name="filter"/>.
        /// </summary>        
        /// <param name="filter"></param>
        /// <param name="categoryFlags"></param>
        /// <param name="flat"></param>
        public virtual async Task UpdateCategories(
            TransactionFilter filter = null,
            CategoryTypes categoryFlags = CategoryTypes.All,
            bool flat = false,
            bool order = false)
        {
            try
            {
                var categoryExpenses = await GetCategoriesAsync(filter, categoryFlags, flat);

                UpdateCategoriesInternal(categoryExpenses, GetComparer(order));
            }
            catch
            {
                StatusMessageService.ShowMessage("Could not get categories", "Retry",
                        async () => await UpdateCategories(filter, categoryFlags, flat, order));
            }
        }

        protected virtual IComparer<TCategoryViewModel> GetComparer(bool order)
        {
            return new CategoryComparer();
        }

        class CategoryComparer : IComparer<TCategoryViewModel>
        {
            public int Compare(TCategoryViewModel x, TCategoryViewModel y)
            {
                if (x.IsNoCategory)
                {
                    return -1;
                }

                if (y.IsNoCategory)
                {
                    return 1;
                }

                if (x.Category == Category.AllCategory)
                {
                    return 1;
                }

                if (y.Category == Category.AllCategory)
                {
                    return -1;
                }

                return x.Category.Name.CompareTo(y.Category.Name);
            }
        }
    }
}