﻿using System.Collections.Generic;
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

namespace Moneyes.UI.ViewModels
{

    class MainViewModel : ViewModelBase
    {
        private LiveDataService _liveDataService;
        private IExpenseIncomeService _expenseIncomeService;
        private TransactionRepository _transactionRepository;

        private AccountRepository _accountRepo;
        private AccountDetails _selectedAccount;
        private CategoryViewModel _selectedCategory;

        private ObservableCollection<AccountDetails> _accounts = new();
        private ObservableCollection<Transaction> _transactions = new();
        private ObservableCollection<CategoryViewModel> _categories = new();

        public ICommand LoadedCommand { get; }
        public ICommand FetchOnlineCommand { get; }
        public ICommand SelectCategoryCommand { get; }

        public CategoryViewModel SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged(nameof(SelectedCategory));
            }
        }

        public AccountDetails SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                _selectedAccount = value;
                OnPropertyChanged(nameof(SelectedAccount));
                FetchTransactions();
            }
        }


        public ObservableCollection<AccountDetails> Accounts
        {
            get => _accounts;
            set
            {
                _accounts = value;

                if (value != null && value.Any())
                {
                    SelectedAccount = value.First();
                }

                OnPropertyChanged(nameof(Accounts));
            }
        }

        public ObservableCollection<CategoryViewModel> Categories
        {
            get => _categories;
            set
            {
                _categories = value;

                OnPropertyChanged(nameof(Accounts));
            }
        }

        public ObservableCollection<Transaction> Transactions
        {
            get => _transactions;
            set
            {
                _transactions = value;

                OnPropertyChanged(nameof(Transactions));
            }
        }

        public MainViewModel(
            LiveDataService liveDataService,
            IExpenseIncomeService expenseIncomeService,
            TransactionRepository transactionService,
            AccountRepository accountRepo,
            BankConnectionStore bankConnections)
        {
            DisplayName = "Overview";

            _liveDataService = liveDataService;
            _accountRepo = accountRepo;
            _expenseIncomeService = expenseIncomeService;
            _transactionRepository = transactionService;

            LoadedCommand = new AsyncCommand(async ct =>
            {
                //TODO: 
                // Get current bank
                // Fetch accounts for this bank

                var bankingDetails = bankConnections.GetBankingDetails();

                if (bankingDetails == null)
                {
                    // No bank connection configured -> show message?
                    return;
                }

                await FetchAccounts(bankingDetails.BankCode.ToString());
            });

            //liveDataService.BankingInitialized += bankingDetails =>
            //{
            //    _ = Task.Run(() => FetchAccounts(bankingDetails.BankCode.ToString()));
            //};

            FetchOnlineCommand = new AsyncCommand(async ct =>
            {
                Result<int> result = await _liveDataService
                    .FetchOnlineTransactions(SelectedAccount);

                if (result.IsSuccessful && result.Data > 0)
                {
                    FetchTransactions();
                }
            });

            SelectCategoryCommand = new AsyncCommand<CategoryViewModel>(async (viewModel, ct) =>
            {
                await Task.Run(() => FetchTransactions(updateCategories: false));
            });
        }

        private void FetchTransactions(bool updateCategories = true)
        {
            //Category[] selectedCategories = SelectedCategory?
            //    .Cast<CategoryViewModel>()
            //    .Select(c => c.Category)
            //    .ToArray();

            Category selectedCategory = SelectedCategory?.Category;

            // Get all transactions for selected category and filter
            IEnumerable<Transaction> transactions = _transactionRepository.All(
                filter: GetTransactionFilter(),
                categories: selectedCategory);

            Transactions = new(transactions);

            if (updateCategories)
            {
                UpdateCategories();
            }
        }

        private TransactionFilter GetTransactionFilter()
        {
            return new TransactionFilter()
            {
                AccountNumber = _selectedAccount.Number
            };
        }

        private void UpdateCategories()
        {
            // Get expenses per category
            _expenseIncomeService.GetExpensePerCategory(SelectedAccount)
                .OnError(() => HandleError("Could not get expenses for this category"))
                .OnSuccess(expenses =>
                {
                    Categories.Clear();

                    foreach ((Category category, decimal amt) in expenses)
                    {
                        Categories.Add(new(category, amt));
                    }

                    // Get total expenses
                    _expenseIncomeService.GetTotalExpense(SelectedAccount)
                        .OnSuccess(totalAmt =>
                        {
                            Categories.Add(new("Total", totalAmt));
                        })
                        .OnError(() => HandleError("Could not get total expense"));

                    // Set sub categories
                    foreach (CategoryViewModel category in Categories)
                    {
                        Category parent = category.Category?.Parent;
                        if ( parent == null) { continue; }

                        // Add category as sub category in parent
                        Categories.FirstOrDefault(c => c.Category.Equals(parent))
                            .SubCatgeories.Add(category);
                    }
                });
        }

        private void HandleError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async Task FetchAccounts(string bankCode)
        {
            Accounts = new(_accountRepo.GetByBankCode(bankCode));

            if (Accounts.Any())
            {
                return;
            }

            Result result = await _liveDataService.FetchAndImportAccounts();

            if (!result.IsSuccessful)
            {
                // TODO: Display message
            }

            Accounts = new(_accountRepo.All());
        }
    }
}
