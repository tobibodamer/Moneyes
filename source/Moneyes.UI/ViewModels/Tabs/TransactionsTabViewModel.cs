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
using Microsoft.Extensions.Logging;

namespace Moneyes.UI.ViewModels
{
    internal class TransactionsTabViewModel : TabViewModelBase
    {
        private readonly IBankingService _bankingService;
        private readonly IStatusMessageService _statusMessageService;
        private readonly ICategoryService _categoryService;
        private Balance _currentBalance;
        private bool _isLoaded;
        private readonly ILogger<TransactionsTabViewModel> _logger;

        private TransactionsViewModel _transactionsViewModel;

        #region UI Properties
        public ICommand LoadedCommand { get; }

        public ExpenseCategoriesViewModel Categories { get; }

        private SelectorViewModel _selector;
        public SelectorViewModel Selector
        {
            get => _selector;
            set
            {
                _selector = value;
                OnPropertyChanged();
            }
        }

        public TransactionsViewModel TransactionsViewModel
        {
            get => _transactionsViewModel;
            set
            {
                _transactionsViewModel = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Transaction> _selectedTransactions;
        public ObservableCollection<Transaction> SelectedTransactions
        {
            get => _selectedTransactions;
            set
            {
                _selectedTransactions = value;
                OnPropertyChanged();
            }
        }

        public Balance CurrentBalance
        {
            get => _currentBalance;
            set
            {
                _currentBalance = value;
                OnPropertyChanged();
            }
        }

        private bool _flatCategories;
        public bool FlatCategories
        {
            get => _flatCategories;
            set
            {
                _flatCategories = value;
                OnPropertyChanged();
                OnFlatDisplayChanged();
            }
        }

        public TransactionFilterViewModel TransactionFilter { get; } = new();

        #endregion
        public TransactionsTabViewModel(
            ICachedRepository<TransactionDbo> transactionRepository,
            ITransactionService transactionService,
            ICategoryService categoryService,
            ICachedRepository<CategoryDbo> categoryRepository,
            IBankingService bankingService,
            IStatusMessageService statusMessageService,
            ExpenseCategoriesViewModel expenseCategoriesViewModel,
            SelectorViewModel selectorViewModel,
            ILogger<TransactionsTabViewModel> logger)
        {
            DisplayName = "Transactions";
            Categories = expenseCategoriesViewModel;
            Selector = selectorViewModel;

            _categoryService = categoryService;
            _bankingService = bankingService;
            _statusMessageService = statusMessageService;

            _logger = logger;

            NeedsUpdate = true;

            Categories.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(Categories.SelectedCategory)
                    && Categories.SelectedCategory is not null
                    && !Categories.IsUpdating)
                {
                    BeginUpdateTransactions();
                }
            };

            categoryRepository.RepositoryChanged += async (e) =>
            {
                if (PostponeUpdate())
                {
                    return;
                }

                _logger.LogDebug("Category repository changed -> Updating categories.");

                await UpdateCategories();

                if (e.Actions.HasFlag(RepositoryChangedAction.Replace)
                 && e.ReplacedItems.Any(c => Categories.IsSelected(c.Id)))
                {
                    _logger.LogDebug("Repository change was a replace action -> Updating transactions");
                    await UpdateTransactions();
                }
            };

            transactionRepository.RepositoryChanged += async (args) =>
            {
                if (PostponeUpdate())
                {
                    return;
                }

                _logger.LogDebug("Transaction repository changed -> Updating categories and transactions");

                await UpdateCategories();
                await UpdateTransactions();
            };

            Selector.SelectorChanged += async (sender, args) =>
            {
                if (PostponeUpdate())
                {
                    return;
                }

                _logger.LogDebug("Selector changed -> Updating categories and transactions");

                await UpdateCategories();
                await UpdateTransactions();
            };

            //Selector.PropertyChanged += async (Sender, args) =>
            //{
            //    if (args.PropertyName == nameof(Selector.EndDate))
            //    {
            //        await UpdateCategories();
            //        await UpdateTransactions();
            //    }
            //};

            TransactionsViewModel = new(transactionService)
            {
                RemoveFromCategory = new CollectionRelayCommand<Transaction>(transactions =>
                {
                    foreach (Transaction t in transactions)
                    {
                        _categoryService.RemoveFromCategory(t);
                    }
                }, transactions =>
                {
                    return transactions != null && transactions.Any(t => t.Category != null);
                })
            };

            TransactionFilter.FilterChanged += (sender, args) =>
            {
                if (!IsActive)
                {
                    return;
                }

                BeginUpdateTransactions();
            };
            _logger = logger;
        }

        private TransactionFilter GetTransactionFilter()
        {
            return new TransactionFilter()
            {
                StartDate = Selector.FromDate,
                EndDate = Selector.EndDate,
                AccountNumber = Selector.CurrentAccount?.Number,
                Criteria = TransactionFilter.IsEnabled ? TransactionFilter.GetFilter() : null
            };
        }

        private void BeginUpdateTransactions()
        {
            UpdateTransactions().FireAndForgetSafeAsync();
        }

        private void OnFlatDisplayChanged()
        {
            UpdateCategories().ContinueWith(t =>
            {
                if (Categories.SelectedCategory != null &&
                (Categories.SelectedCategory.IsRealCategory ||
                Categories.SelectedCategory.IsNoCategory))
                {
                    BeginUpdateTransactions();
                }
            }).FireAndForgetSafeAsync();
        }

        private async Task UpdateTransactions()
        {
            if (Selector.CurrentAccount != null)
            {
                CurrentBalance = _bankingService.GetBalance(Selector.EndDate, Selector.CurrentAccount);
            }

            Category selectedCategory = Categories.SelectedCategory?.Category;

            if (FlatCategories)
            {
                await TransactionsViewModel.UpdateTransactions(GetTransactionFilter(), selectedCategory);
            }
            else
            {
                List<Category> categories = new(_categoryService.GetSubCategories(selectedCategory));

                categories.Add(selectedCategory);

                await TransactionsViewModel.UpdateTransactions(GetTransactionFilter(), categories.ToArray());
            }

        }

        private async Task UpdateCategories()
        {
            await Categories.UpdateCategories(GetTransactionFilter(), flat: FlatCategories);
        }

        public void Refresh()
        {
            UpdateCategories().ContinueWith(async t =>
                {
                    await UpdateTransactions();
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
                        BeginUpdateTransactions();
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