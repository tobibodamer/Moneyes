using Microsoft.Extensions.Logging;
using Moneyes.Core;
using Moneyes.Data;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace Moneyes.UI
{
    public class LiveDataService
    {
        private readonly TransactionRepository _transactionRepo;
        private readonly IBaseRepository<AccountDetails> _accountRepo;
        private readonly BalanceRepository _balanceRepo;

        private readonly ICategoryService _categoryService;

        private readonly OnlineBankingServiceFactory _bankingServiceFactory;
        private readonly IPasswordPrompt _passwordProvider;
        private readonly BankConnectionStore _configStore;
        private readonly ILogger<LiveDataService> _logger;

        private OnlineBankingService _bankingService;

        public event Action<OnlineBankingDetails> BankingInitialized;

        public LiveDataService(
            TransactionRepository transactionStore,
            ICategoryService categoryService,
            IBaseRepository<AccountDetails> accountRepo,
            BalanceRepository balanceRepo,
            BankConnectionStore bankConnectionStore,
            OnlineBankingServiceFactory bankingServiceFactory,
            IPasswordPrompt passwordPrompt)
        {
            _transactionRepo = transactionStore;
            _categoryService = categoryService;
            _accountRepo = accountRepo;
            _balanceRepo = balanceRepo;
            _configStore = bankConnectionStore;
            _bankingServiceFactory = bankingServiceFactory;
            _passwordProvider = passwordPrompt;
        }

        private static DateTime FirstOfMonth => new(DateTime.Now.Year, DateTime.Now.Month, 1);

        private async Task EnsurePassword()
        {
            if (_bankingService.BankingDetails.Pin.IsNullOrEmpty())
            {
                SecureString password = await _passwordProvider.WaitForPasswordAsync();

                if (password == null || password.Length == 0)
                {
                    throw new OperationCanceledException();
                }

                _bankingService.BankingDetails.Pin = password;
            }
        }

        public Result<IBankInstitute> FindBank(int bankCode)
        {
            try
            {
                IBankInstitute bank = BankInstitutes.GetInstitute(bankCode);

                if (bank != null)
                {
                    return Result.Successful(bank);
                }
            }
            catch
            {
                //TODO: Log
            }

            return Result.Failed<IBankInstitute>();
        }

        public async Task<Result> CreateBankConnection(OnlineBankingDetails bankingDetails, bool sync = false)
        {
            try
            {
                _logger?.LogInformation("Creating bank connection, bank code '{bankCode}'",
                    bankingDetails.BankCode);

                _bankingService = _bankingServiceFactory.CreateService(bankingDetails);

                if (sync)
                {
                    await EnsurePassword();

                    return await _bankingService.Sync();
                }

                BankingInitialized?.Invoke(bankingDetails);

                return Result.Successful();
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error while creating bank connection", ex);

                _bankingService = null;
            }

            return Result.Failed();
        }

        private async Task<Result> InitOnlineBankingService()
        {
            _logger?.LogInformation("Initializing online banking service");

            // Get current banking settings from store
            OnlineBankingDetails bankingDetails = _configStore.GetBankingDetails();

            if (bankingDetails == null)
            {
                _logger?.LogWarning("Cannot initialize online banking service. No bank configuration available.");

                return Result.Failed();
            }

            if (_bankingService == null ||
                !_bankingService.BankingDetails.BankCode.Equals(bankingDetails.BankCode))
            {
                // Bank changed or not initialized, create new banking connection
                Result createConnectionResult = await CreateBankConnection(bankingDetails, sync: true);

                if (!createConnectionResult.IsSuccessful)
                {
                    return createConnectionResult;
                }
            }

            _logger?.LogInformation("Applying current banking settings");

            // Apply banking settings from store
            _bankingService.BankingDetails.UserId = bankingDetails.UserId;

            if (!bankingDetails.Pin.IsNullOrEmpty())
            {
                _bankingService.BankingDetails.Pin = bankingDetails.Pin;
            }

            _logger?.LogInformation("Online banking service initialized");

            return Result.Successful();
        }

        public async Task<Result<int>> FetchOnlineTransactionsAndBalances(
            AccountDetails account, 
            AssignMethod categoryAssignMethod = AssignMethod.KeepPrevious)
        {
            try
            {
                Result initResult = await InitOnlineBankingService();

                if (!initResult.IsSuccessful)
                {
                    //return initResult;
                    return Result.Failed<int>();
                }

                await EnsurePassword();

                Result<TransactionData> result =
                    await _bankingService.Transactions(account, startDate: FirstOfMonth, endDate: DateTime.Now);

                if (!result.IsSuccessful)
                {
                    return Result.Failed<int>();
                }

                // Transactions and Balances
                List<Transaction> transactions = result.Data.Transactions.ToList();
                List<Balance> balances = result.Data.Balances.ToList();

                // Assign categories
                _categoryService.AssignCategories(transactions, assignMethod: categoryAssignMethod, updateDatabase: false);

                // Store
                _transactionRepo.Set(transactions);
                _balanceRepo.Set(balances);

                return transactions.Count;
            }
            catch
            {
                //TODO: LOG
            }

            return Result.Failed<int>();
        }

        public async Task<Result> FetchAndImportAccounts()
        {
            try
            {
                Result initResult = await InitOnlineBankingService();

                if (!initResult.IsSuccessful)
                {
                    //return initResult;
                    return Result.Failed<int>();
                }

                await EnsurePassword();

                IEnumerable<AccountDetails> accounts = (await _bankingService.Accounts()).GetOrNull();

                if (accounts != null)
                {
                    _accountRepo.Set(accounts);

                    return Result.Successful(accounts);
                }
            }
            catch (Exception ex)
            {
                //TODO: Log
            }

            return Result.Failed();
        }

        public async Task<Result<IEnumerable<AccountDetails>>> FetchAccounts()
        {
            try
            {
                Result initResult = await InitOnlineBankingService();

                if (!initResult.IsSuccessful)
                {
                    return Result.Failed<IEnumerable<AccountDetails>>();
                }

                await EnsurePassword();

                IEnumerable<AccountDetails> accounts = (await _bankingService.Accounts()).GetOrNull();

                return Result.Successful(accounts);
            }
            catch (Exception ex)
            {
                //TODO: Log
            }

            return Result.Failed<IEnumerable<AccountDetails>>();
        }
    }
}
