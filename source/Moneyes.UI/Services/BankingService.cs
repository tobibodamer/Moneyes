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

        public BankingService(IBankConnectionStore bankConnectionStore,
            AccountRepository accountRepository, BalanceRepository balanceRepository)
        {
            _bankConnectionStore = bankConnectionStore;
            _accountRepository = accountRepository;
            _balanceRepository = balanceRepository;

            accountRepository.EntityAdded += AccountRepository_EntityAdded;
        }

        private void AccountRepository_EntityAdded(AccountDetails account)
        {
            NewAccount?.Invoke(account);
        }

        public bool HasBankingDetails => _bankConnectionStore.HasBankingDetails;

        public OnlineBankingDetails BankingDetails
        {
            get => _bankConnectionStore.GetBankingDetails();
            set => _bankConnectionStore.SetBankingDetails(value);
        }

        public event Action<AccountDetails> NewAccount;

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
            int counter = 0;

            foreach (var account in accounts)
            {
                if (_accountRepository.Set(account))
                {
                    counter++;
                }
            }

            return counter;
        }

        public Balance GetBalance(DateTime date, AccountDetails? account)
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
    }
}