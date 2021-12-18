using Moneyes.Core;
using Moneyes.Data;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Moneyes.UI
{
    public class BankingService : IBankingService
    {
        private readonly IBankConnectionStore _bankConnectionStore;
        private readonly AccountRepository _accountRepository;
        private readonly BalanceRepository _balanceRepository;
        private readonly TransactionRepository _transactionRepository;
        private readonly ICategoryService _categoryService;

        public BankingService(
            IBankConnectionStore bankConnectionStore,
            AccountRepository accountRepository,
            BalanceRepository balanceRepository,
            TransactionRepository transactionRepository,
            ICategoryService categoryService)
        {
            _bankConnectionStore = bankConnectionStore;
            _accountRepository = accountRepository;
            _balanceRepository = balanceRepository;
            _transactionRepository = transactionRepository;
            _categoryService = categoryService;
        }

        public bool HasBankingDetails => _bankConnectionStore.HasBankingDetails;

        public void UpdateBankingDetails(Action<OnlineBankingDetails> update)
        {
            update(BankingDetails);

            _bankConnectionStore.SetBankingDetails(BankingDetails);
        }

        public OnlineBankingDetails BankingDetails
        {
            get => _bankConnectionStore.GetBankingDetails();
            set => _bankConnectionStore.SetBankingDetails(value);
        }

        public event Action NewAccountsImported;

        public IEnumerable<AccountDetails> GetAccounts()
        {
            if (!HasBankingDetails)
            {
                return Enumerable.Empty<AccountDetails>();
            }

            return _accountRepository.GetByBankCode(BankingDetails.BankCode.ToString());
        }

        public int ImportAccounts(IEnumerable<AccountDetails> accounts)
        {
            int numAccountsAdded = _accountRepository.Set(accounts);

            //foreach (var account in accounts)
            //{
            //    if (_accountRepository.Set(account))
            //    {
            //        numAccountsAdded++;
            //    }
            //}

            NewAccountsImported?.Invoke();

            return numAccountsAdded;
        }

        public Balance GetBalance(DateTime date, AccountDetails account)
        {
            //TODO: implement total balance
            if (account == null)
            {
                foreach (var acc in GetAccounts())
                {
                    return _balanceRepository.GetByDate(date, acc);
                }

                return null;
            }
            else
            {
                return _balanceRepository.GetByDate(date, account);
            }
        }

        public int ImportTransactions(IEnumerable<Transaction> transactions, AssignMethod categoryAssignMethod)
        {
            // Assign categories
            _categoryService.AssignCategories(transactions, assignMethod: categoryAssignMethod, updateDatabase: false);

            // Store
            return _transactionRepository.Set(transactions);
        }

        public int ImportBalances(IEnumerable<Balance> balances)
        {
            return _balanceRepository.Set(balances);
        }
    }
}