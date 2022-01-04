using Microsoft.Extensions.Logging;
using Moneyes.BankService.Dtos;
using Moneyes.Core;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.UI.Services
{
    class LiveDataServiceREST : ILiveDataService
    {
        private readonly IBankingService _bankingService;

        private readonly IPasswordPrompt _passwordProvider;
        private readonly IStatusMessageService _statusMessageService;

        private readonly ILogger<LiveDataServiceREST> _logger;

        private readonly static Uri _bankServiceApiUrl = new("https://localhost:44385/");
        private readonly HttpClient _httpClient = new() { BaseAddress = _bankServiceApiUrl };

        private bool _isAuthenticated = false;
        private OnlineBankingDetails _authenticatedDetails;
        private string _authenticatedPasswordHash;

        public event Action<OnlineBankingDetails> BankingInitialized;

        public LiveDataServiceREST(
            IBankingService bankingService,
            IPasswordPrompt passwordPrompt,
            IStatusMessageService statusMessageService,
            ILogger<LiveDataServiceREST> logger)
        {
            _bankingService = bankingService;
            _passwordProvider = passwordPrompt;
            _statusMessageService = statusMessageService;
            _logger = logger;
        }

        private async Task InitOnlineBankingService()
        {
            _logger?.LogInformation("Initializing online banking service");

            // Get current banking settings from store
            OnlineBankingDetails bankingDetails = _bankingService.BankingDetails.Copy();

            if (bankingDetails == null)
            {
                _logger?.LogWarning("Cannot initialize online banking service. No bank configuration available.");

                throw new InvalidOperationException("No online banking details stored.");
            }

            if (DetailsChangedSinceAuth(bankingDetails, comparePassword: true))
            {
                _logger?.LogInformation("Bank with code {bankCode} not initialized, creating now",
                    bankingDetails.BankCode);

                bool savePassword = false;

                // No PIN is provided -> request
                if (bankingDetails.Pin.IsNullOrEmpty())
                {
                    (SecureString password, bool save) = await _passwordProvider.WaitForPasswordAsync();

                    if (password.IsNullOrEmpty())
                    {
                        _logger?.LogWarning("Password request cancelled, cancelling operation");

                        // Status notification?
                        throw new OperationCanceledException();
                    }

                    // PIN provided
                    bankingDetails.Pin = password;
                    savePassword = save;
                }

                // Bank changed or not initialized, create new banking connection
                // (will throw on fail -> ok to discard result as its only for sync)
                var result = await CreateBankConnection(bankingDetails, testConnection: false);

                // Save PIN if successful
                if (savePassword && result.IsSuccessful)
                {
                    _bankingService.UpdateBankingDetails(existingBankingDetails =>
                    {
                        existingBankingDetails.Pin = bankingDetails.Pin;
                    });

                    _statusMessageService.ShowMessage("Password saved");
                    _logger?.LogDebug("Password saved");
                }
            }

            _logger?.LogInformation("Online banking service initialized");
        }

        /// <summary>
        /// Compares the authenticated details to the current details, and returns true if they differ.
        /// </summary>
        /// <param name="currentDetails"></param>
        /// <param name="comparePassword"></param>
        /// <returns></returns>
        private bool DetailsChangedSinceAuth(OnlineBankingDetails currentDetails, bool comparePassword = false)
        {
            OnlineBankingDetails bankingDetails = currentDetails;

            if (!_isAuthenticated)
            {
                return true;
            }
            
            if (!_authenticatedDetails.BankCode.Equals(bankingDetails.BankCode))
            {
                return true;
            }

            if (!_authenticatedDetails.Server?.Equals(bankingDetails.Server) ?? bankingDetails.Server is not null)
            {
                return true;
            }

            if (!_authenticatedDetails.UserId.Equals(bankingDetails.UserId))
            {
                return true;
            }

            if (comparePassword
                && !bankingDetails.Pin.IsNullOrEmpty() && _authenticatedPasswordHash != null
                && bankingDetails.Pin.MatchWithHash(_authenticatedPasswordHash))
            {
                return true;
            }

            return false;
        }

        private async Task<HttpResponseMessage> CreateBankConnectionInternal(OnlineBankingDetails bankingDetails, bool testConnection = false)
        {
            return await _httpClient.PostAsJsonAsync("/login", new CreateConnectionDto
            {
                BankCode = bankingDetails.BankCode,
                Server = bankingDetails.Server,
                UserId = bankingDetails.UserId,
                Pin = bankingDetails.Pin.ToUnsecuredString(),
                TestConnection = testConnection
            });
        }
        public async Task<Result> CreateBankConnection(OnlineBankingDetails bankingDetails, bool testConnection = false)
        {
            try
            {
                _logger?.LogInformation("Creating bank connection, bank code '{bankCode}'",
                    bankingDetails.BankCode);

                var response = await CreateBankConnectionInternal(bankingDetails, testConnection);

                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation("Bank connection created, bank code '{bankCode}'", bankingDetails.BankCode);

                    _isAuthenticated = true;
                    _authenticatedDetails = new()
                    {
                        BankCode = bankingDetails.BankCode,
                        Server = bankingDetails.Server,
                        UserId = bankingDetails.UserId
                    };
                    _authenticatedPasswordHash = bankingDetails.Pin?.Hash();

                    if (DetailsChangedSinceAuth(bankingDetails, comparePassword: false))
                    {
                        BankingInitialized?.Invoke(bankingDetails);
                    }

                    return Result.Successful();
                }

                _logger?.LogWarning("Bank connection not created. Server responded with status code {statusCode}",
                    (int)response.StatusCode);

            }
            catch (Exception ex)
            {
                _logger?.LogError("Error while creating bank connection", ex);
                _isAuthenticated = false;
                throw;
            }

            _isAuthenticated = false;
            return Result.Failed();
        }

        private async Task<HttpResponseMessage> EnsurePassword(Func<Task<HttpResponseMessage>> operation, int maxRetries = 3)
        {
            int numRetries = 0;

            if (_isAuthenticated)
            {
                _logger?.LogDebug("Password already set, trying to perform operation");
                // Password is set -> try to perform operation

                (HttpResponseMessage response, bool wrongPassword) = await TryRequest(operation, isPasswordAuthenticated: true);

                if (!wrongPassword)
                {
                    return response;
                }

                // reset authentication flag
                _isAuthenticated = false;

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
                
                // Create new banking details with provided password
                var bankingDetails = _bankingService.BankingDetails.Copy();
                bankingDetails.Pin = password;

                // Create bank connection without testing
                var loginResponse = await CreateBankConnectionInternal(bankingDetails, testConnection: false);

                if (!loginResponse.IsSuccessStatusCode)
                {
                    return loginResponse;
                }

                (HttpResponseMessage response, bool wrongPassword) = await TryRequest(operation, isPasswordAuthenticated: false);

                if (wrongPassword)
                {
                    // Operation failed because of wrong password -> next try
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    // Operation failed otherwise -> return
                    return response;
                }

                _logger?.LogInformation("Password ensured");

                // Operation successful -> password ensured

                _isAuthenticated = true;
                _authenticatedDetails = new()
                {
                    BankCode = bankingDetails.BankCode,
                    Server = bankingDetails.Server,
                    UserId = bankingDetails.UserId
                };
                _authenticatedPasswordHash = bankingDetails.Pin?.Hash();

                if (DetailsChangedSinceAuth(bankingDetails, comparePassword: false))
                {
                    BankingInitialized?.Invoke(bankingDetails);
                }

                if (savePassword)
                {
                    _bankingService.UpdateBankingDetails(bankingDetails =>
                    {
                        bankingDetails.Pin = password;
                    });

                    _statusMessageService.ShowMessage("Password saved");

                    _logger?.LogDebug("Password saved");
                }

                return response;
            }

            _logger?.LogDebug("Password request failed after {n} tries. Cancelling operation", maxRetries);

            // Clear wrong password after all retries failed
            _isAuthenticated = false;

            throw new OperationCanceledException();
        }

        public async Task<(HttpResponseMessage response, bool wrongPassword)> TryRequest(Func<Task<HttpResponseMessage>> sendRequest, 
            bool isPasswordAuthenticated)
        {
            var response = await sendRequest();

            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized)
            {
                _logger?.LogError("Bank service responded with status code 'Unauthorized (401)'");

                if (!isPasswordAuthenticated)
                {
                    _statusMessageService.ShowMessage("Invalid username or PIN");
                }

                return (null, true);
            }
            else if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Bank service responded with {statusCode}: \n {response}", 
                    response.StatusCode, await response.Content.ReadAsStringAsync());
            }

            return (response, false);
        }

        public async Task<Result<IEnumerable<AccountDetails>>> FetchAccounts()
        {
            try
            {
                await InitOnlineBankingService();

                var response = await EnsurePassword(async () =>
                    await _httpClient.GetAsync("/accounts"));

                if (response.IsSuccessStatusCode)
                {
                    var accounts = await response.Content.ReadFromJsonAsync<IEnumerable<AccountDetails>>();

                    return Result.Successful(accounts);
                }

                _logger?.LogWarning("Bank connection not created. Server responded with status code {statusCode}",
                    (int)response.StatusCode);

            }
            catch (Exception ex)
            {
                //TODO: Log
            }

            return Result.Failed<IEnumerable<AccountDetails>>();
        }

        public async Task<Result> FetchAndImportAccounts()
        {
            try
            {
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

        public async Task<Result<int>> FetchTransactionsAndBalances(AccountDetails account, AssignMethod categoryAssignMethod = AssignMethod.KeepPrevious)
        {
            try
            {
                await InitOnlineBankingService();

                var response = await EnsurePassword(async () =>
                {
                    string uri = $"/transactions?AccountNumber={account.Number}&IBAN={account.IBAN}" +
                    $"&StartDate={FirstOfMonth:yyyy-MM-dd}&EndDate={DateTime.Now:yyyy-MM-dd}";

                    return await _httpClient.GetAsync(uri);
                });

                if (!response.IsSuccessStatusCode)
                {
                    return Result.Failed<int>();
                }

                var transactionData = await response.Content.ReadFromJsonAsync<TransactionBalanceListDto>();

                // Transactions and Balances
                List<Transaction> transactions = new(transactionData.Transactions.Select(t => new Transaction()
                {
                    AltName = t.AltName,
                    Amount = t.Amount,
                    BookingType = t.BookingType,
                    BIC = t.BIC,
                    BookingDate = t.BookingDate,
                    Currency = t.Currency,
                    IBAN = t.IBAN,
                    Index = t.Index,
                    Name = t.Name,
                    PartnerIBAN = t.PartnerIBAN,
                    Purpose = t.Purpose,
                    ValueDate = t.ValueDate
                }));

                List<Balance> balances = new(transactionData.Balances.Select(b => new Balance()
                {
                    Date = b.Date,
                    Amount = b.Amount,
                    Account = account,
                    Currency = transactions.First()?.Currency
                }));

                int numTransactionsAdded = _bankingService.ImportTransactions(transactions, categoryAssignMethod);
                int numBalancesAdded = _bankingService.ImportBalances(balances);

                return numTransactionsAdded;
            }
            catch (Exception ex)
            {
                //TODO: log
            }

            return Result.Failed<int>();
        }
        private static DateTime FirstOfMonth => new(DateTime.Now.Year, DateTime.Now.Month, 1);
        public Task<Result<int>> FetchTransactionsAndBalances(AccountDetails[] accounts, AssignMethod categoryAssignMethod = AssignMethod.KeepPrevious)
        {
            throw new NotImplementedException();
        }
        public IBankInstitute FindBank(int bankCode)
        {
            if (!BankInstitutes.IsSupported(bankCode))
            {
                _logger?.LogWarning("Bank institute (bank code '{bankCode}') is not supported.", bankCode);

                return null;
            }

            return BankInstitutes.GetInstitute(bankCode);
        }
    }
}
