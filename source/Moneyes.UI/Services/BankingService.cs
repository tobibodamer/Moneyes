using Microsoft.Extensions.Logging;
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
        private readonly IUniqueCachedRepository<BankDbo> _bankDetailsRepository;
        private readonly IUniqueCachedRepository<AccountDbo> _accountRepository;
        private readonly IUniqueCachedRepository<BalanceDbo> _balanceRepository;
        private readonly ITransactionService _transactionService;
        private readonly ICategoryService _categoryService;

        private readonly IAccountFactory _accountFactory;
        private readonly IBalanceFactory _balanceFactory;
        private readonly IBankDetailsFactory _bankDetailsFactory;

        private readonly ILogger<BankingService> _logger;

        public BankingService(
            IUniqueCachedRepository<BankDbo> bankDetailsRepository,
            IUniqueCachedRepository<AccountDbo> accountRepository,
            IUniqueCachedRepository<BalanceDbo> balanceRepository,
            ITransactionService transactionService,
            ICategoryService categoryService,
            IAccountFactory accountFactory,
            IBalanceFactory balanceFactory,
            IBankDetailsFactory bankDetailsFactory,
            ILogger<BankingService> logger)
        {
            _bankDetailsRepository = bankDetailsRepository;
            _accountRepository = accountRepository;
            _balanceRepository = balanceRepository;
            _transactionService = transactionService;
            _categoryService = categoryService;
            _accountFactory = accountFactory;
            _balanceFactory = balanceFactory;
            _bankDetailsFactory = bankDetailsFactory;
            _logger = logger;
        }

        public event Action NewAccountsImported;

        public IReadOnlyList<BankDetails> GetBankEntries()
        {
            return _bankDetailsRepository
                .Select(b => _bankDetailsFactory.CreateFromDbo(b))
                .ToList();
        }

        public IReadOnlyList<AccountDetails> GetAllAccounts()
        {
            return _accountRepository
                .Select(a => _accountFactory.CreateFromDbo(a))
                .ToList();
        }

        public IReadOnlyList<AccountDetails> GetAccounts(BankDetails bankDetails)
        {
            return _accountRepository
                .Where(acc => acc.Bank.Id.Equals(bankDetails.Id))
                .Select(a => _accountFactory.CreateFromDbo(a))
                .ToList();
        }

        public int ImportAccounts(IEnumerable<AccountDetails> accounts)
        {
            try
            {
                _logger.LogInformation("Importing accounts...");

                int numAccountsAdded = _accountRepository.SetMany(
                    entities: accounts.Select(acc => acc.ToDbo()),
                    onConflict: x => x.UpdateContentOrIgnore());

                if (numAccountsAdded > 0)
                {
                    NewAccountsImported?.Invoke();
                }

                return numAccountsAdded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while importing accounts:");
                throw;
            }
        }

        public Balance GetBalance(DateTime date, AccountDetails account)
        {
            //TODO: implement total balance            
            return GetBalanceByDate(date, account);
        }

        public decimal GetOverallBalance(DateTime date)
        {
            return GetAllAccounts()
                .Select(acc => GetBalanceByDate(date, acc))
                .Where(x => x != null)
                .Sum(b => b.Amount);
        }

        /// <summary>
        /// Gets the balance that is closed to the given <paramref name="date"/>.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public Balance GetBalanceByDate(DateTime date, AccountDetails account)
        {
            var dbo = _balanceRepository
                .Where(b => b.Account.Id.Equals(account.Id))
                .Where(b => b.Date <= date)
                .OrderByDescending(b => b.Date)
                .FirstOrDefault();

            if (dbo == null)
            {
                return null;
            }

            return _balanceFactory.CreateFromDbo(dbo);
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
            return _transactionService.ImportTransactions(transactions);
        }

        public int ImportBalances(IEnumerable<Balance> balances)
        {
            return _balanceRepository.SetMany(
                entities: balances.Select(x => x.ToDbo()),
                onConflict: factory => factory.UpdateContentOrIgnore());
        }

        public void AddBankConnection(BankDetails bankDetails)
        {
            try
            {
                var dbo = bankDetails.ToDbo();

                _bankDetailsRepository.Create(dbo);
            }
            catch (ConstraintViolationException ex)
            {
                if (ex.PropertyName.Equals(nameof(CategoryDbo.Name)))
                {
                    // Name already exists
                }
            }
            catch (DuplicateKeyException)
            {
                // Primary key already exists
            }
        }

        public void UpdateBankConnection(BankDetails bankDetails)
        {
            ArgumentNullException.ThrowIfNull(bankDetails, nameof(bankDetails));

            _bankDetailsRepository.Update(bankDetails.ToDbo(),
                onConflict: v => ConflictResolutionAction.Fail());
        }


    }
}