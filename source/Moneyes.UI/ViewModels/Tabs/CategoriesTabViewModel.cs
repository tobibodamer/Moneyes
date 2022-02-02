using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System;
using Moneyes.LiveData;
using Moneyes.Core;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using Moneyes.Core.Filters;
using Moneyes.Data;
using System.Windows;
using System.Collections;
using System.Threading;
using Moneyes.UI.View;
using Moneyes.UI.Services;
using System.Diagnostics;

namespace Moneyes.UI.ViewModels
{
    internal class CategoriesTabViewModel : TabViewModelBase
    {
        private readonly IBankingService _bankingService;
        private readonly IStatusMessageService _statusMessageService;
        private readonly ICategoryService _categoryService;

        private bool _isLoaded;

        #region UI Properties
        public ExpenseCategoriesViewModel Categories { get; }


        private bool _flatCategories;
        public bool FlatCategories
        {
            get => _flatCategories;
            set
            {
                _flatCategories = value;
                OnPropertyChanged();
                UpdateCategories().FireAndForgetSafeAsync();
            }
        }

        #endregion
        public CategoriesTabViewModel(
            IUniqueCachedRepository<TransactionDbo> transactionRepository,
            ICachedRepository<CategoryDbo> categoryRepository,
            IBankingService bankingService,
            ICategoryService categoryService,
            IStatusMessageService statusMessageService,
            ExpenseCategoriesViewModel expenseCategoriesViewModel,
            SelectorViewModel selectorViewModel)
        {
            DisplayName = "Categories";
            Categories = expenseCategoriesViewModel;
            _categoryService = categoryService;
            _bankingService = bankingService;
            _statusMessageService = statusMessageService;

            NeedsUpdate = true;

            Categories.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(Categories.SelectedCategory))
                {
                    //Categories.SelectedCategory.EditCommand.Execute(null);
                }
            };

            categoryRepository.RepositoryChanged += async (e) =>
            {
                if (PostponeUpdate())
                {
                    return;
                }

                await UpdateCategories();
            };
        }
        private async Task UpdateCategories()
        {
            await Categories.UpdateCategories(new(), flat: FlatCategories);
        }

        public void Refresh()
        {
            UpdateCategories().ContinueWith(async t =>
                {
                    NeedsUpdate = false;
                })
                .FireAndForgetSafeAsync();
        }

        public override void OnSelect()
        {
            base.OnSelect();

            if (!_isLoaded)
            {
                UpdateCategories().ContinueWith(t =>
                    {
                        _isLoaded = true;
                    })
                    .FireAndForgetSafeAsync();
            }
            else if (NeedsUpdate)
            {
                Refresh();
            }
        }
    }
}