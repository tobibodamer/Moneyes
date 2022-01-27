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
        private readonly ICachedRepository<BankDetails> _bankDetailsRepository;
        private readonly ICachedRepository<AccountDetails> _accountRepository;
        private readonly ICachedRepository<Balance> _balanceRepository;
        private readonly IUniqueCachedRepository<Transaction> _transactionRepository;
        private readonly ICategoryService _categoryService;

        public BankingService(
            ICachedRepository<BankDetails> bankDetailsRepository,
            ICachedRepository<AccountDetails> accountRepository,
            ICachedRepository<Balance> balanceRepository,
            IUniqueCachedRepository<Transaction> transactionRepository,
            ICategoryService categoryService)
        {
            _bankDetailsRepository = bankDetailsRepository;
            _accountRepository = accountRepository;
            _balanceRepository = balanceRepository;
            _transactionRepository = transactionRepository;
            _categoryService = categoryService;
        }

        public bool HasBankingDetails => _bankDetailsRepository.GetAll().Any();

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

                var bankDetails = _bankDetailsRepository.GetAll().FirstOrDefault();

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

                _bankDetailsRepository.Set(bankDetails);
            }
        }

        public event Action NewAccountsImported;

        public IEnumerable<AccountDetails> GetAccounts()
        {
            if (!HasBankingDetails)
            {
                return Enumerable.Empty<AccountDetails>();
            }

            return _accountRepository.GetAll().Where(acc => acc.BankCode.Equals(BankingDetails.BankCode.ToString()));
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
                    return GetBalanceByDate(date, acc);
                }

                return null;
            }
            else
            {
                return GetBalanceByDate(date, account);
            }
        }

        /// <summary>
        /// Gets the balance that is closed to the given <paramref name="date"/>.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public Balance GetBalanceByDate(DateTime date, AccountDetails account)
        {
            return _balanceRepository.GetAll()
                .Where(b => b.Account.Id.Equals(account.Id))
                .Where(b => b.Date <= date)
                .OrderByDescending(b => b.Date)
                .FirstOrDefault();
        }

        //public Balance GetBalanceByDate(DateTime date, string bankCode)
        //{
        //    ArgumentNullException.ThrowIfNull(bankCode);

        //    var grouped = _balanceRepository.GetAll()
        //        .Where(b => b.Account != null && 
        //                    (b.Account.BankCode?.Equals(bankCode) ?? false))
        //        .Where(b => b.Date <= date)
        //        .OrderByDescending(b => b.Date)
        //        .GroupBy(b => b.Account);

        //    var minDate = grouped.Min(g => g.Select(b => b.Date).FirstOrDefault());

        //    return minDate;
        //}

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