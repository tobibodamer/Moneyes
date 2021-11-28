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
    internal class TransactionsTabViewModel : ViewModelBase, ITabViewModel
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

            Categories.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(Categories.SelectedCategory))
                {
                    UpdateTransactions();
                }
            };

            categoryRepository.RepositoryChanged += (args) =>
            {
                UpdateCategories();

                if (args.Actions.HasFlag(RepositoryChangedAction.Replace)
                 && args.ReplacedItems.Any(c => Categories.IsSelected(c)))
                {
                    UpdateTransactions();
                }
            };

            transactionRepository.RepositoryChanged += (args) =>
            {
                UpdateCategories();
                UpdateTransactions();
            };

            Selector.SelectorChanged += (sender, args) =>
            {
                UpdateCategories();
                UpdateTransactions();
            };

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
        }

        private TransactionFilter GetTransactionFilter()
        {
            return new TransactionFilter()
            {
                StartDate = Selector.FromDate,
                EndDate = Selector.EndDate,
                AccountNumber = Selector.CurrentAccount?.Number
            };
        }

        private void UpdateTransactions()
        {
            _ = Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                Category selectedCategory = Categories.SelectedCategory?.Category;

                //var withSubCategories = _categoryService.GetSubCategories(selectedCategory)
                //    .Concat(new Category[] { selectedCategory });

                TransactionsViewModel.UpdateTransactions(GetTransactionFilter(), selectedCategory);

                if (Selector.CurrentAccount != null)
                {
                    CurrentBalance = _bankingService.GetBalance(Selector.EndDate, Selector.CurrentAccount);
                }
            });
        }

        private void UpdateCategories()
        {
            _ = Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                Categories.UpdateCategories(GetTransactionFilter());
            });
        }

        public void OnSelect()
        {
            Selector.RefreshAccounts();

            if (!_isLoaded)
            {
                UpdateCategories();
                UpdateTransactions();

                _isLoaded = true;
            }
        }
    }
}