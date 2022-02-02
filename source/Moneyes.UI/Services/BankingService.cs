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
        private readonly ICachedRepository<BankDbo> _bankDetailsRepository;
        private readonly ICachedRepository<AccountDbo> _accountRepository;
        private readonly ICachedRepository<BalanceDbo> _balanceRepository;
        private readonly ITransactionService _transactionService;
        private readonly ICategoryService _categoryService;

        private readonly IAccountFactory _accountFactory;
        private readonly IBalanceFactory _balanceFactory;
        private readonly IBankDetailsFactory _bankDetailsFactory;

        public BankingService(
            ICachedRepository<BankDbo> bankDetailsRepository,
            ICachedRepository<AccountDbo> accountRepository,
            ICachedRepository<BalanceDbo> balanceRepository,
            ITransactionService transactionService,
            ICategoryService categoryService,
            IAccountFactory accountFactory,
            IBalanceFactory balanceFactory,
            IBankDetailsFactory bankDetailsFactory)
        {
            _bankDetailsRepository = bankDetailsRepository;
            _accountRepository = accountRepository;
            _balanceRepository = balanceRepository;
            _transactionService = transactionService;
            _categoryService = categoryService;
            _accountFactory = accountFactory;
            _balanceFactory = balanceFactory;
            _bankDetailsFactory = bankDetailsFactory;
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
                if (BankDetails == null)
                {
                    return null;
                }

                return new()
                {
                    UserId = BankDetails.UserId,
                    Server = BankDetails.Server,
                    BankCode = BankDetails.BankCode,
                    Pin = BankDetails.Pin
                };
            }
            set
            {
                BankDetails bankDetails = new(BankDetails?.Id ?? Guid.NewGuid(), value.BankCode)
                {
                    UserId = value.UserId,
                    Server = value.Server,
                    BankCode = value.BankCode,
                    Pin = value.Pin,
                };

                BankDetails = bankDetails;
            }
        }

        private BankDetails BankDetails
        {
            get
            {
                var dbo = _bankDetailsRepository.GetAll().FirstOrDefault();
                if (dbo == null)
                {
                    return null;
                }

                return _bankDetailsFactory.CreateFromDbo(dbo);
            }
            set
            {
                _bankDetailsRepository.Set(value.ToDbo());
            }
        }

        public event Action NewAccountsImported;

        public IEnumerable<BankDetails> GetBankEntries()
        {
            return _bankDetailsRepository.GetAll()
                .Select(b => _bankDetailsFactory.CreateFromDbo(b))
                .ToList();
        }

        public IEnumerable<AccountDetails> GetAccounts()
        {
            if (!HasBankingDetails)
            {
                return Enumerable.Empty<AccountDetails>();
            }

            return _accountRepository.GetAll()
                .Where(acc => acc.Bank.Id.Equals(BankDetails.Id))
                .Select(a => _accountFactory.CreateFromDbo(a))
                .ToList();
        }

        public int ImportAccounts(IEnumerable<AccountDetails> accounts)
        {
            var dbos = accounts.Select(b =>
            {
                if (!_accountRepository.Contains(b.Id))
                {
                    return b.ToDbo(createdAt: DateTime.Now, updatedAt: DateTime.Now);
                }
                else
                {
                    return b.ToDbo(updatedAt: DateTime.Now);
                }
            }).ToList();

            int numAccountsAdded = _accountRepository.Set(dbos, onConflict: v =>
            {
                if (AccountDbo.ContentEquals(v.ExistingEntity, v.NewEntity))
                {
                    return ConflictResolutionAction.Ignore();
                }

                var updateAccount = new AccountDbo()
                {
                    Id = v.ExistingEntity.Id,
                    CreatedAt = v.ExistingEntity.CreatedAt,
                    UpdatedAt = DateTime.Now,
                    IsDeleted = v.NewEntity.IsDeleted,
                    IBAN = v.NewEntity.IBAN,
                    Number = v.NewEntity.Number,
                    OwnerName = v.NewEntity.OwnerName,
                    Type = v.NewEntity.Type,
                    Bank = v.NewEntity.Bank
                };

                return ConflictResolutionAction.Update(updateAccount);
            });

            if (numAccountsAdded > 0)
            {
                NewAccountsImported?.Invoke();
            }

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
            var dbo = _balanceRepository.GetAll()
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
            var dbos = balances.Select(b =>
            {
                if (!_balanceRepository.Contains(b.Id))
                {
                    return b.ToDbo(createdAt: DateTime.Now, updatedAt: DateTime.Now);
                }
                else
                {
                    return b.ToDbo(updatedAt: DateTime.Now);
                }
            }).ToList();

            return _balanceRepository.Set(dbos, onConflict: v =>
            {
                if (BalanceDbo.ContentEquals(v.ExistingEntity, v.NewEntity))
                {
                    return ConflictResolutionAction.Ignore();
                }

                var update = new BalanceDbo()
                {
                    Id = v.ExistingEntity.Id,
                    CreatedAt = v.ExistingEntity.CreatedAt,
                    UpdatedAt = v.NewEntity.UpdatedAt,
                    IsDeleted = v.NewEntity.IsDeleted,
                    Date = v.NewEntity.Date,
                    Amount = v.NewEntity.Amount,
                    Account = v.NewEntity.Account,
                    Currency = v.NewEntity.Currency,
                };

                return ConflictResolutionAction.Update(update);
            });
        }
    }
}