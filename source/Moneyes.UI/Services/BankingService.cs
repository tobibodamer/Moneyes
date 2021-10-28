﻿using Moneyes.Core;
using Moneyes.Data;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Moneyes.UI
{
    public class BankingService : IBankingService
    {
        private readonly BankConnectionStore _bankConnectionStore;
        private readonly AccountRepository _accountRepository;
        private readonly BalanceRepository _balanceRepository;

        public BankingService(BankConnectionStore bankConnectionStore, 
            AccountRepository accountRepository, BalanceRepository balanceRepository)
        {
            _bankConnectionStore = bankConnectionStore;
            _accountRepository = accountRepository;
            _balanceRepository = balanceRepository;
        }

        public bool HasBankingDetails => _bankConnectionStore.HasBankingDetails;

        public OnlineBankingDetails BankingDetails
        {
            get => _bankConnectionStore.GetBankingDetails();
            set => _bankConnectionStore.SetBankingDetails(value);
        }

        public IEnumerable<AccountDetails> GetAccounts()
        {
            if (!HasBankingDetails)
            {
                return Enumerable.Empty<AccountDetails>();
            }

            return _accountRepository.GetByBankCode(BankingDetails.BankCode.ToString());
        }

        public Balance GetBalance(DateTime date, AccountDetails account)
        {
            return _balanceRepository.GetByDate(date, account);
        }
    }
}