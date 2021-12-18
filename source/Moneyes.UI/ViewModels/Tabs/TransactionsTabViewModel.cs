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
    internal class TransactionsTabViewModel : TabViewModelBase
    {
        private readonly IBankingService _bankingService;
        private readonly IStatusMessageService _statusMessageService;
        private readonly ICategoryService _categoryService;
        private Balance _currentBalance;
        private bool _isLoaded;

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

        public TransactionFilterViewModel TransactionFilter { get; } = new();

        #endregion
        public TransactionsTabViewModel(
            TransactionRepository transactionRepository,
            CategoryRepository categoryRepository,
            IBankingService bankingService,
            ICategoryService categoryService,
            IStatusMessageService statusMessageService,
            ExpenseCategoriesViewModel expenseCategoriesViewModel,
            SelectorViewModel selectorViewModel)
        {
            DisplayName = "Transactions";
            Categories = expenseCategoriesViewModel;
            Selector = selectorViewModel;

            _categoryService = categoryService;
            _bankingService = bankingService;
            _statusMessageService = statusMessageService;

            NeedsUpdate = true;

            Categories.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(Categories.SelectedCategory) && Categories.SelectedCategory is not null)
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

                await UpdateCategories();

                if (e.Actions.HasFlag(RepositoryChangedAction.Replace)
                 && e.ReplacedItems.Any(c => Categories.IsSelected(c)))
                {
                    await UpdateTransactions();
                }
            };

            transactionRepository.RepositoryChanged += async (args) =>
            {
                if (PostponeUpdate())
                {
                    return;
                }

                await UpdateCategories();
                await UpdateTransactions();
            };

            Selector.SelectorChanged += async (sender, args) =>
            {
                if (PostponeUpdate())
                {
                    return;
                }

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

            TransactionsViewModel = new(transactionRepository)
            {
                RemoveFromCategory = new CollectionRelayCommand<Transaction>(transactions =>
                {
                    Category selectedCategory = Categories.SelectedCategory.Category;

                    foreach (Transaction t in transactions)
                    {
                        if (t.Categories.Contains(selectedCategory))
                        {
                            _categoryService.RemoveFromCategory(t, selectedCategory);
                        }
                    }
                }, transactions =>
                {
                    return transactions != null
                        && transactions.All(transaction => transaction != null &&
                            Categories.SelectedCategory != null &&
                            Categories.SelectedCategory.IsRealCategory &&
                            transaction.Categories.Contains(Categories.SelectedCategory.Category));
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
        }

        private TransactionFilter GetTransactionFilter()
        {
            return new TransactionFilter()
            {
                StartDate = Selector.FromDate,
                EndDate = Selector.EndDate,
                AccountNumber = Selector.CurrentAccount?.Number,
                Criteria = TransactionFilter.GetFilter()
            };
        }

        private void BeginUpdateTransactions()
        {
            UpdateTransactions().FireAndForgetSafeAsync();
        }

        private async Task UpdateTransactions()
        {
            if (Selector.CurrentAccount != null)
            {
                CurrentBalance = _bankingService.GetBalance(Selector.EndDate, Selector.CurrentAccount);
            }

            Category selectedCategory = Categories.SelectedCategory?.Category;

            //var withSubCategories = _categoryService.GetSubCategories(selectedCategory)
            //    .Concat(new Category[] { selectedCategory });

            await TransactionsViewModel.UpdateTransactions(GetTransactionFilter(), selectedCategory);
        }

        private async Task UpdateCategories()
        {
            await Categories.UpdateCategories(GetTransactionFilter());
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