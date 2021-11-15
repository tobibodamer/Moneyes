using Microsoft.Extensions.Logging;
using Moneyes.Core;
using Moneyes.Data;
using Moneyes.LiveData;
using Moneyes.UI.Services;
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

        private readonly IOnlineBankingServiceFactory _bankingServiceFactory;
        private readonly IPasswordPrompt _passwordProvider;
        private readonly IBankConnectionStore _configStore;
        private readonly ILogger<LiveDataService> _logger;

        private readonly IStatusMessageService _statusMessageService;

        private IOnlineBankingService _bankingService;

        public event Action<OnlineBankingDetails> BankingInitialized;

        public LiveDataService(
            TransactionRepository transactionStore,
            ICategoryService categoryService,
            IBaseRepository<AccountDetails> accountRepo,
            BalanceRepository balanceRepo,
            IBankConnectionStore bankConnectionStore,
            IOnlineBankingServiceFactory bankingServiceFactory,
            IPasswordPrompt passwordPrompt,
            IStatusMessageService statusMessageService)
        {
            _transactionRepo = transactionStore;
            _categoryService = categoryService;
            _accountRepo = accountRepo;
            _balanceRepo = balanceRepo;
            _configStore = bankConnectionStore;
            _bankingServiceFactory = bankingServiceFactory;
            _passwordProvider = passwordPrompt;
            _statusMessageService = statusMessageService;
        }

        private static DateTime FirstOfMonth => new(DateTime.Now.Year, DateTime.Now.Month, 1);

        /// <summary>
        /// Ensures a valid password is set: <br></br>
        /// If a password is already stored in cache, accept. <br></br>
        /// If no password is set, request with <paramref name="maxRetries"/> retries.
        /// </summary>
        /// <param name="maxRetries"></param>
        /// <returns></returns>
        private async Task EnsurePassword(int maxRetries = 3)
        {
            await EnsurePassword(_bankingService.Sync, maxRetries);
        }

        private async Task EnsurePassword(Func<Task> operation, int maxRetries = 3)
        {
            _ = await EnsurePassword<int>(async () =>
              {
                  await operation();

                  return 0;
              }, maxRetries);
        }

        private async Task<TResult> EnsurePassword<TResult>(Func<Task<TResult>> operation, int maxRetries = 3)
        {
            int numRetries = 0;

            if (!_bankingService.BankingDetails.Pin.IsNullOrEmpty())
            {
                // Password is set -> try to perform operation

                (TResult result, bool successful) = await TryOperation(operation);

                if (successful)
                {
                    // Operation successful
                    return result;
                }

                numRetries++;
            }

            // Password not set -> try to request it
            for (; numRetries < maxRetries; numRetries++)
            {
                (SecureString password, bool savePassword) = await _passwordProvider.WaitForPasswordAsync();

                if (password.IsNullOrEmpty())
                {
                    // Status notification?
                    throw new OperationCanceledException();
                }

                // Set password and try to perform operation
                _bankingService.BankingDetails.Pin = password;

                (TResult result, bool successful) = await TryOperation(operation);

                if (!successful)
                {
                    // Operation failed -> next try
                    continue;
                }

                // Operation successful -> password ensured

                if (savePassword)
                {
                    OnlineBankingDetails bankingDetails = _configStore.GetBankingDetails();

                    bankingDetails.Pin = password;

                    _configStore.SetBankingDetails(bankingDetails);
                    _statusMessageService.ShowMessage("Password saved");
                }

                return result;
            }

            // Clear wrong password after all retries failed
            _bankingService.BankingDetails.Pin = null;

            throw new OperationCanceledException();
        }

        /// <summary>
        /// Tries to perform an operation while handling wrong credentials.
        /// </summary>
        /// <param name="operation"></param>
        /// <returns>Whether the operation was executed successfully.</returns>
        private async Task<(TResult Result, bool Successful)> TryOperation<TResult>(Func<Task<TResult>> operation)
        {
            try
            {
                return (await operation(), true);
            }
            catch (OnlineBankingException ex)
            when (ex.ErrorCode is OnlineBankingErrorCode.InvalidPin
                or OnlineBankingErrorCode.InvalidUsernameOrPin)
            {
                _logger?.LogError(ex, "Invalid username or password");

                _statusMessageService.ShowMessage("Invalid username or PIN");

                _bankingService.BankingDetails.Pin = null;
            }
            catch (OnlineBankingException ex)
            {
                _logger?.LogError(ex, "Error during operation:");

                if (ex.Message != null)
                {
                    _statusMessageService.ShowMessage(ex.Message);
                }

                _bankingService.BankingDetails.Pin = null;

                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during operation:");

                // Clear wrong password when something else went wrong
                _bankingService.BankingDetails.Pin = null;

                throw;
            }

            return (default, false);
        }

        private async Task<bool> VerifyCredentials()
        {
            try
            {
                await _bankingService.Sync();

                _logger?.LogInformation("Sync successful");

                return true;
            }
            catch (OnlineBankingException ex)
            when (ex.ErrorCode is OnlineBankingErrorCode.InvalidPin
                or OnlineBankingErrorCode.InvalidUsernameOrPin)
            {
                _logger?.LogError(ex, "Invalid username or password");

                // Invalid password -> ?
                _statusMessageService.ShowMessage("Invalid username or PIN");
            }
            catch (OnlineBankingException ex)
            {
                _logger?.LogError(ex, "Error during sync:");

                if (ex.Message != null)
                {
                    _statusMessageService.ShowMessage(ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during sync:");

                // Clear wrong password when something else went wrong
                _bankingService.BankingDetails.Pin = null;

                throw;
            }

            return false;
        }

        /// <summary>
        /// Finds the bank institute with the given bank code.
        /// </summary>
        /// <param name="bankCode"></param>
        /// <returns></returns>
        public IBankInstitute FindBank(int bankCode)
        {
            if (!BankInstitutes.IsSupported(bankCode))
            {
                _logger?.LogWarning("Bank institute (bank code '{bankCode}') is not supported.", bankCode);
                throw new NotSupportedException($"Bank institute (bank code '{bankCode}') is not supported.");
            }

            return BankInstitutes.GetInstitute(bankCode);
        }

        /// <summary>
        /// Creates a bank connection with the given online banking details and tests the connection.
        /// </summary>
        /// <param name="bankingDetails">The banking details used for the connection</param>
        /// <param name="testConnection">If true performs a sync to test the connection</param>
        /// <returns></returns>
        public async Task CreateBankConnection(OnlineBankingDetails bankingDetails, bool testConnection = false)
        {
            try
            {
                _logger?.LogInformation("Creating bank connection, bank code '{bankCode}'",
                    bankingDetails.BankCode);

                _bankingService = _bankingServiceFactory.CreateService(bankingDetails);

                if (testConnection)
                {
                    _logger?.LogInformation("Testing bank connection");

                    // Try to sync -> will throw exception when credentials wrong
                    await _bankingService.Sync();
                }

                _logger?.LogInformation("Bank connection created, bank code '{bankCode}'", bankingDetails.BankCode);

                BankingInitialized?.Invoke(bankingDetails);
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error while creating bank connection", ex);

                _bankingService = null;
                throw;
            }
        }

        /// <summary>
        /// Initializes a online banking service with the <see cref="OnlineBankingDetails"/> from the store,
        /// or updates the credentials of an existing connection.
        /// </summary>
        /// <returns></returns>
        private async Task InitOnlineBankingService()
        {
            _logger?.LogInformation("Initializing online banking service");

            // Get current banking settings from store
            OnlineBankingDetails bankingDetails = _configStore.GetBankingDetails();

            if (bankingDetails == null)
            {
                _logger?.LogWarning("Cannot initialize online banking service. No bank configuration available.");

                throw new InvalidOperationException("No online banking details stored.");
            }

            if (_bankingService == null ||
                !_bankingService.BankingDetails.BankCode.Equals(bankingDetails.BankCode))
            {
                _logger?.LogInformation("Bank with code {bankCode} not initialized, creating now",
                    bankingDetails.BankCode);

                // Bank changed or not initialized, create new banking connection
                await CreateBankConnection(bankingDetails, testConnection: false);

                _logger?.LogInformation("Applying current banking settings");

                // Apply credentials from store
                _bankingService.BankingDetails.UserId = bankingDetails.UserId;
                _bankingService.BankingDetails.Pin = bankingDetails.Pin;
            }
            else
            {
                // Apply credentials from store if not set

                _logger?.LogInformation("Applying current banking settings");

                if (string.IsNullOrEmpty(_bankingService.BankingDetails.UserId))
                {
                    _bankingService.BankingDetails.UserId = bankingDetails.UserId;
                }

                if (_bankingService.BankingDetails.Pin.IsNullOrEmpty())
                {
                    _bankingService.BankingDetails.Pin = bankingDetails.Pin;
                }
            }

            _logger?.LogInformation("Online banking service initialized");
        }

        public async Task<Result<int>> FetchTransactionsAndBalances(
            AccountDetails account,
            AssignMethod categoryAssignMethod = AssignMethod.KeepPrevious)
        {
            try
            {
                await InitOnlineBankingService();

                Result<TransactionData> result = await EnsurePassword(async () =>
                            await _bankingService.Transactions(account, startDate: FirstOfMonth, endDate: DateTime.Now));

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
                int numTransactionsAdded = _transactionRepo.Set(transactions);
                _balanceRepo.Set(balances);

                return numTransactionsAdded;

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
                await InitOnlineBankingService();

                IEnumerable<AccountDetails> accounts = await EnsurePassword(async () =>
                    (await _bankingService.Accounts()).GetOrNull());

                if (accounts != null)
                {
                    _ = _accountRepo.Set(accounts);

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
                await InitOnlineBankingService();

                IEnumerable<AccountDetails> accounts = await EnsurePassword(async () => 
                    (await _bankingService.Accounts()).GetOrNull());

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
