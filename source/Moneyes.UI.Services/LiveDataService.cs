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
        private readonly IBankingService _bankingService;

        private readonly IOnlineBankingService _onlineBankingService;

        private readonly IPasswordPrompt _passwordProvider;
        private readonly IStatusMessageService _statusMessageService;

        private readonly ILogger<LiveDataService> _logger;


        private readonly Dictionary<Guid, SecureString> _localPins = new();


        public LiveDataService(
            IBankingService bankingService,
            IOnlineBankingServiceFactory bankingServiceFactory,
            IPasswordPrompt passwordPrompt,
            IStatusMessageService statusMessageService,
            ILogger<LiveDataService> logger)
        {
            _bankingService = bankingService;
            _passwordProvider = passwordPrompt;
            _statusMessageService = statusMessageService;
            _logger = logger;
            _onlineBankingService = bankingServiceFactory.CreateService();
        }

        private async Task<TResult> EnsurePassword<TResult>(BankDetails bankDetails, Func<SecureString, Task<TResult>> operation, int maxRetries = 3)
            where TResult : BankingResult
        {
            _logger?.LogDebug("Ensuring password for {bankCode}", bankDetails.BankCode);

            int numRetries = 0;

            SecureString password = bankDetails.Pin;

            // Password set -> try operation
            if (!password.IsNullOrEmpty() || _localPins.TryGetValue(bankDetails.Id, out password!))
            {
                _logger?.LogDebug("Password found for {id}, trying to perform operation", bankDetails.Id);

                (TResult? result, bool wrongPassword) = await TryOperation(password!, operation);

                if (!wrongPassword)
                {
                    return result!;
                }

                numRetries++;
            }

            // Password not set -> try to request it
            for (; numRetries < maxRetries; numRetries++)
            {
                _logger?.LogDebug("Requesting password ({try}/{total})", numRetries + 1, maxRetries);

                (password, bool savePassword) = await _passwordProvider.WaitForPasswordAsync();

                if (password.IsNullOrEmpty())
                {
                    _logger?.LogWarning("Password request cancelled, cancelling operation");

                    // Status notification?
                    throw new OperationCanceledException();
                }

                (TResult? result, bool wrongPassword) = await TryOperation(password, operation);

                if (wrongPassword)
                {
                    // Operation failed because of wrong password -> next try
                    continue;
                }

                _logger?.LogInformation("Password ensured");

                // Store working password in temp store
                _localPins[bankDetails.Id] = password;

                if (!result!.IsSuccessful)
                {
                    // Operation failed otherwise -> return
                    return result;
                }

                // Operation successful -> password ensured

                if (savePassword)
                {
                    bankDetails.Pin = password;
                    _bankingService.UpdateBankConnection(bankDetails);

                    _statusMessageService.ShowMessage("Password saved");
                    _logger?.LogDebug("Password saved");
                }

                return result;
            }

            _logger?.LogDebug("Password request failed after {n} tries. Cancelling operation", maxRetries);

            // Clear wrong password from temp store after all tries failed
            _localPins.Remove(bankDetails.Id);

            throw new OperationCanceledException();
        }

        /// <summary>
        /// Tries to perform an operation while handling wrong credentials.
        /// </summary>
        /// <param name="operation"></param>
        /// <returns>Whether the operation was executed successfully.</returns>
        private async Task<(TResult? result, bool wrongPassword)> TryOperation<TResult>(SecureString password, Func<SecureString, Task<TResult>> operation)
            where TResult : BankingResult
        {
            var result = await operation(password);

            if (!result.IsSuccessful &&
                result.ErrorCode is OnlineBankingErrorCode.InvalidUsernameOrPin
                                 or OnlineBankingErrorCode.InvalidPin)
            {
                _logger?.LogError("Operation failed: Invalid username or password");

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


        public async Task<BankingResult> TestConnection(OnlineBankingDetails onlineBankingDetails)
        {
            _logger?.LogInformation("Testing bank connection");

            // Try to sync
            var result = await _onlineBankingService.Sync(onlineBankingDetails);

            return result;
        }

        public void SavePasswordTemporarily(BankDetails bankDetails, SecureString password)
        {
            _localPins[bankDetails.Id] = password;
        }

        /// <summary>
        /// Initializes a online banking service with the <see cref="OnlineBankingDetails"/> from the store,
        /// or updates the credentials of an existing connection.
        /// </summary>
        /// <returns></returns>
        private OnlineBankingDetails CreateOnlineBankingDetails(BankDetails bankDetails, SecureString password)
        {
            var bankServer = bankDetails.Server;

            if (bankServer is null)
            {
                var bank = FindBank(bankDetails.BankCode);
                if (bank != null)
                {
                    bankServer = bank.FinTs_Url;
                }
            }
            OnlineBankingDetails onlineBankingDetails = new()
            {
                BankCode = bankDetails.BankCode,
                Server = new Uri(bankServer),
                UserId = bankDetails.UserId,
                Pin = password
            };

            return onlineBankingDetails;
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
            List<Transaction> transactions = new();
            List<Balance> balances = new();
            List<Result> results = new();

            foreach (var account in accounts)
            {
                var bankDetails = account.BankDetails;

                // Account has no transaction permission -> fetch balances directly

                BankingResult<Balance> balanceResult = await EnsurePassword(bankDetails,
                    async (password) =>
                    {
                        return await _onlineBankingService.Balance(
                            onlineBankingDetails: CreateOnlineBankingDetails(bankDetails, password),
                            account: account);
                    });

                results.Add(balanceResult);

                if (!balanceResult.IsSuccessful)
                {
                    continue;
                }

                balances.Add(balanceResult.Data);

                if (!_onlineBankingService.CanFetchTransactions(account))
                {
                    continue;
                }

                BankingResult<TransactionData> result = await EnsurePassword(bankDetails,
                    async (password) =>
                    {
                        return await _onlineBankingService.Transactions(
                            onlineBankingDetails: CreateOnlineBankingDetails(bankDetails, password),
                            account: account,
                            startDate: DateTime.Now.AddDays(-30),
                            endDate: DateTime.Now);
                    });

                results.Add(result);

                if (!result.IsSuccessful)
                {
                    continue;
                }

                // Transactions and Balances
                transactions.AddRange(result.Data.Transactions);
                balances.AddRange(result.Data.Balances);
            }

            if (results.All(r => !r.IsSuccessful))
            {
                return Result.Failed<int>();
            }

            try
            {
                int numTransactionsAdded = _bankingService.ImportTransactions(transactions, categoryAssignMethod);
                int numBalancesAdded = _bankingService.ImportBalances(balances);

                return numTransactionsAdded;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Importing transactions or balances failed:");
            }

            return Result.Failed<int>();
        }

        public async Task<Result<IEnumerable<AccountDetails>>> FetchAccounts(BankDetails bankDetails)
        {
            try
            {
                BankingResult<IEnumerable<AccountDetails>> result = await EnsurePassword(bankDetails,
                    async (password) =>
                    {
                        return await _onlineBankingService.Accounts(
                            onlineBankingDetails: CreateOnlineBankingDetails(bankDetails, password),
                            bank: bankDetails);
                    });

                if (!result.IsSuccessful)
                {
                    return Result.Failed<IEnumerable<AccountDetails>>();
                }

                var accounts = result.Data;


                return Result.Successful(accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching accounts:");
            }

            return Result.Failed<IEnumerable<AccountDetails>>();
        }
    }
}
