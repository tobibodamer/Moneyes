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
        private readonly IBankingService _bankingService;

        private readonly IOnlineBankingServiceFactory _bankingServiceFactory;
        private IOnlineBankingService _onlineBankingService;
        
        private readonly IPasswordPrompt _passwordProvider;
        private readonly IStatusMessageService _statusMessageService;

        private readonly ILogger<LiveDataService> _logger;

        public event Action<OnlineBankingDetails> BankingInitialized;
        
        public LiveDataService(
            IBankingService bankingService,
            IOnlineBankingServiceFactory bankingServiceFactory,
            IPasswordPrompt passwordPrompt,
            IStatusMessageService statusMessageService,
            ILogger<LiveDataService> logger)
        {
            _bankingService = bankingService;
            _bankingServiceFactory = bankingServiceFactory;
            _passwordProvider = passwordPrompt;
            _statusMessageService = statusMessageService;
            _logger = logger;
        }

        private static DateTime FirstOfMonth => new(DateTime.Now.Year, DateTime.Now.Month, 1);

        /// <summary>
        /// Ensures a valid password is set: <br></br>
        /// If a password is already stored in cache, accept. <br></br>
        /// If no password is set, request with <paramref name="maxRetries"/> retries.
        /// </summary>
        /// <param name="maxRetries"></param>
        /// <returns></returns>
        public async Task EnsurePassword(int maxRetries = 3)
        {
            await EnsurePassword(_onlineBankingService.Sync, maxRetries);
        }

        private async Task<TResult> EnsurePassword<TResult>(Func<Task<TResult>> operation, int maxRetries = 3)
            where TResult : BankingResult
        {
            int numRetries = 0;

            if (!_onlineBankingService.BankingDetails.Pin.IsNullOrEmpty())
            {
                _logger?.LogDebug("Password already set, trying to perform operation");
                // Password is set -> try to perform operation

                (TResult result, bool wrongPassword) = await TryOperation(operation);

                if (!wrongPassword)
                {
                    return result;
                }

                numRetries++;
            }

            // Password not set -> try to request it
            for (; numRetries < maxRetries; numRetries++)
            {
                _logger?.LogDebug("Requesting password ({try}/{total})", numRetries + 1, maxRetries);

                (SecureString password, bool savePassword) = await _passwordProvider.WaitForPasswordAsync();

                if (password.IsNullOrEmpty())
                {
                    _logger?.LogWarning("Password request cancelled, cancelling operation");

                    // Status notification?
                    throw new OperationCanceledException();
                }

                // Set password and try to perform operation
                _onlineBankingService.BankingDetails.Pin = password;

                (TResult result, bool wrongPassword) = await TryOperation(operation);

                if (wrongPassword)
                {
                    // Operation failed because of wrong password -> next try
                    continue;
                }

                if (!result.IsSuccessful)
                {
                    // Operation failed otherwise -> return
                    return result;
                }

                _logger?.LogInformation("Password ensured");

                // Operation successful -> password ensured

                if (savePassword)
                {
                    _bankingService.UpdateBankingDetails(bankingDetails =>
                    {
                        bankingDetails.Pin = password;
                    });

                    _statusMessageService.ShowMessage("Password saved");

                    _logger?.LogDebug("Password saved");
                }

                return result;
            }

            _logger?.LogDebug("Password request failed after {n} tries. Cancelling operation", maxRetries);

            // Clear wrong password after all retries failed
            _onlineBankingService.BankingDetails.Pin = null;

            throw new OperationCanceledException();
        }

        /// <summary>
        /// Tries to perform an operation while handling wrong credentials.
        /// </summary>
        /// <param name="operation"></param>
        /// <returns>Whether the operation was executed successfully.</returns>
        private async Task<(TResult result, bool wrongPassword)> TryOperation<TResult>(Func<Task<TResult>> operation)
            where TResult : BankingResult
        {
            var result = await operation();

            if (!result.IsSuccessful &&
                result.ErrorCode is OnlineBankingErrorCode.InvalidUsernameOrPin
                                 or OnlineBankingErrorCode.InvalidPin)
            {
                _logger?.LogError("Invalid username or password");

                _statusMessageService.ShowMessage("Invalid username or PIN");

                return (null, true);
            }

            return (result, false);
        }

#nullable enable
        /// <summary>
        /// Finds the bank institute with the given bank code.
        /// </summary>
        /// <param name="bankCode"></param>
        /// <returns></returns>
        public IBankInstitute? FindBank(int bankCode)
        {
            if (!BankInstitutes.IsSupported(bankCode))
            {
                _logger?.LogWarning("Bank institute (bank code '{bankCode}') is not supported.", bankCode);
                return null;
                //throw new NotSupportedException($"Bank institute (bank code '{bankCode}') is not supported.");
            }

            return BankInstitutes.GetInstitute(bankCode);
        }
#nullable disable

        /// <summary>
        /// Creates a bank connection with the given online banking details and tests the connection.
        /// </summary>
        /// <param name="bankingDetails">The banking details used for the connection</param>
        /// <param name="testConnection">If true performs a sync to test the connection</param>
        /// <returns></returns>
        public async Task<Result> CreateBankConnection(OnlineBankingDetails bankingDetails, bool testConnection = false)
        {
            try
            {
                OnlineBankingDetails bankingDetailsCopy = bankingDetails.Copy();

                _logger?.LogInformation("Creating bank connection, bank code '{bankCode}'",
                    bankingDetails.BankCode);

                _onlineBankingService = _bankingServiceFactory.CreateService(bankingDetailsCopy);

                if (testConnection)
                {
                    _logger?.LogInformation("Testing bank connection");

                    // Try to sync
                    var result = await _onlineBankingService.Sync();

                    if (!result.IsSuccessful)
                    {
                        _logger?.LogWarning("Bank connection wont be created, sync failed");
                        _onlineBankingService = null;

                        return Result.Failed();
                    }
                }

                _logger?.LogInformation("Bank connection created, bank code '{bankCode}'", bankingDetailsCopy.BankCode);

                BankingInitialized?.Invoke(bankingDetailsCopy);

                return Result.Successful();
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error while creating bank connection", ex);

                _onlineBankingService = null;
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
            OnlineBankingDetails bankingDetails = _bankingService.BankingDetails;

            if (bankingDetails == null)
            {
                _logger?.LogWarning("Cannot initialize online banking service. No bank configuration available.");

                throw new InvalidOperationException("No online banking details stored.");
            }

            if (_onlineBankingService == null ||
                !_onlineBankingService.BankingDetails.BankCode.Equals(bankingDetails.BankCode) ||
                _onlineBankingService.BankingDetails.Server != null &&
                !_onlineBankingService.BankingDetails.Server.Equals(bankingDetails.Server))
            {
                _logger?.LogInformation("Bank with code {bankCode} not initialized, creating now",
                    bankingDetails.BankCode);

                // Bank changed or not initialized, create new banking connection
                // (will throw on fail -> ok to discard result as its only for sync)
                _ = await CreateBankConnection(bankingDetails, testConnection: false);
            }
            else
            {
                // Apply credentials from store if not set

                _logger?.LogInformation("Applying current banking credentials");

                if (string.IsNullOrEmpty(_onlineBankingService.BankingDetails.UserId))
                {
                    _onlineBankingService.BankingDetails.UserId = bankingDetails.UserId;
                }

                if (_onlineBankingService.BankingDetails.Pin.IsNullOrEmpty())
                {
                    _onlineBankingService.BankingDetails.Pin = bankingDetails.Pin;
                }
            }

            _logger?.LogInformation("Online banking service initialized");
        }

        public async Task<Result<int>> FetchTransactionsAndBalances(
            AccountDetails account,
            AssignMethod categoryAssignMethod = AssignMethod.KeepPrevious)
        {
            return await FetchTransactionsAndBalances(new AccountDetails[] { account }, categoryAssignMethod);
        }

        public async Task<Result<int>> FetchTransactionsAndBalances(
            AccountDetails[] accounts,
            AssignMethod categoryAssignMethod = AssignMethod.KeepPrevious)
        {
            await InitOnlineBankingService();

            List<Transaction> transactions = new();
            List<Balance> balances = new();

            foreach (var account in accounts)
            {
                BankingResult<TransactionData> result = await EnsurePassword(async () =>
                            await _onlineBankingService.Transactions(account, startDate: DateTime.Now.AddDays(-30), endDate: DateTime.Now));

                if (!result.IsSuccessful)
                {
                    continue;
                    //return Result.Failed<int>();
                }

                // Transactions and Balances
                transactions.AddRange(result.Data.Transactions);
                balances.AddRange(result.Data.Balances);
            }

            if (!transactions.Any())
            {
                return Result.Failed<int>();
            }

            int numTransactionsAdded = _bankingService.ImportTransactions(transactions, categoryAssignMethod);
            int numBalancesAdded = _bankingService.ImportBalances(balances);

            return numTransactionsAdded;
        }

        public async Task<Result> FetchAndImportAccounts()
        {
            try
            {
                await InitOnlineBankingService();

                var result = await FetchAccounts();

                if (!result.IsSuccessful)
                {
                    return result;
                }

                var accounts = result.Data;

                if (accounts != null)
                {
                    int numAccountsAdded = _bankingService.ImportAccounts(accounts);

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

                var result = await EnsurePassword(async () =>
                    await _onlineBankingService.Accounts(_bankingService.GetBankEntries().First()));

                if (!result.IsSuccessful)
                {
                    return Result.Failed<IEnumerable<AccountDetails>>();
                }

                var accounts = result.Data;


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
