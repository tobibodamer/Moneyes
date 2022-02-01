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
        private readonly BankDetailsRepository _bankConnectionStore;
        private readonly AccountRepository _accountRepository;
        private readonly BalanceRepository _balanceRepository;
        private readonly TransactionRepository _transactionRepository;
        private readonly ICategoryService _categoryService;

        public BankingService(
            BankDetailsRepository bankConnectionStore,
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

        public bool HasBankingDetails => _bankConnectionStore.GetAll().Any();

        public void UpdateBankingDetails(Action<OnlineBankingDetails> update)
        {
            var onlineBankinDetails = BankingDetails;

            update(onlineBankinDetails);

            // This copy stuff is all temorary. TODO: replace everything with BankDetails

            BankingDetails = onlineBankinDetails;
        }

        public OnlineBankingDetails BankingDetails
        {
            get
            {

                var bankDetails = _bankConnectionStore.GetAll().FirstOrDefault();

                return new()
                {
                    UserId = bankDetails.UserId,
                    Server = bankDetails.Server,
                    BankCode = bankDetails.BankCode,
                    Pin = bankDetails.Pin
                };
            }
            set
            {
                BankDetails bankDetails = new()
                {
                    Id = new Guid("5be1438e-c03c-47c3-a798-051d36deb338"),
                    UserId = value.UserId,
                    Server = value.Server,
                    BankCode = value.BankCode,
                    Pin = value.Pin
                };

                _bankConnectionStore.Set(bankDetails);
            }
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