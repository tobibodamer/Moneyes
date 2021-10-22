using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using libfintx.FinTS;
using MTransaction = Moneyes.Core.Transaction;
using libfintx.FinTS.Swift;
using Moneyes.Core;

namespace Moneyes.LiveData
{
    /// <summary>
    /// Service that supports basic online banking read operations.
    /// </summary>
    public class OnlineBankingService
    {
        private readonly IFinTsClient _fintsClient;
        private readonly ILogger<OnlineBankingService> _logger;

        /// <summary>
        /// Gets the online banking details used by this service.
        /// </summary>
        public OnlineBankingDetails BankingDetails { get; }

        internal OnlineBankingService(IFinTsClient client, OnlineBankingDetails bankingDetails,
            ILogger<OnlineBankingService> logger = null)
        {
            _fintsClient = client;
            _logger = logger;
            BankingDetails = bankingDetails;
        }

        private void UpdateConnectionDetails(AccountDetails account = null)
        {
            if (account != null)
            {
                _fintsClient.ConnectionDetails.Account = account.Number;
                _fintsClient.ConnectionDetails.Bic = account.BIC;
                _fintsClient.ConnectionDetails.Iban = account.IBAN;
            }

            _fintsClient.ConnectionDetails.Blz = BankingDetails.BankCode;
            _fintsClient.ConnectionDetails.UserId = BankingDetails.UserId;
            _fintsClient.ConnectionDetails.Pin = BankingDetails.Pin;
        }

        private void ValidateBankingDetails()
        {
            //TODO: Pin Prompt

            if (BankingDetails.Pin == null || BankingDetails.Pin.Length == 0)
            {
                throw new ApplicationException("Online banking details must contain valid pin.");
            }
            else if (string.IsNullOrEmpty(BankingDetails.UserId))
            {
                throw new ApplicationException("Online banking details must contain valid user id.");
            }
        }

        /// <summary>
        /// Synchronizes with the online banking API. <br></br>
        /// Can be used to check whether a successful connection can be established to the bank.
        /// </summary>
        /// <returns></returns>
        public async Task<Result> Sync()
        {
            ValidateBankingDetails();

            _logger?.LogInformation("Synchronizing...");

            UpdateConnectionDetails();

            try
            {
                HBCIDialogResult<string> result = await _fintsClient.Synchronization();

                HBCIOutput(result.Messages);

                if (!result.IsSuccess)
                {
                    _logger?.LogWarning("Synchronizing was not successful");

                    return Result.Failed();
                }

                return Result.Successful();
            }
            catch
            {
                //TODO: Log
            }

            return Result.Failed();
        }
        public async Task<Result<IEnumerable<AccountDetails>>> Accounts()
        {
            ValidateBankingDetails();

            _logger?.LogInformation("Fetching account information");

            UpdateConnectionDetails();

            var result = await _fintsClient.Accounts(new(WaitForTanAsync));

            HBCIOutput(result.Messages);

            if (!result.IsSuccess)
            {
                _logger?.LogWarning("Fetching account information was not successful");

                return new(successful: false);
            }

            return Result.Successful(result.Data.Select(accInfo =>
            {
                return new AccountDetails
                {
                    BankCode = accInfo.AccountBankCode,
                    BIC = accInfo.AccountBic,
                    IBAN = accInfo.AccountIban,
                    Number = accInfo.AccountNumber,
                    OwnerName = accInfo.AccountOwner,
                    Type = accInfo.AccountType
                };
            }));
        }

        public async Task<Result<IEnumerable<MTransaction>>> Transactions(
            AccountDetails account,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            ValidateBankingDetails();

            _logger?.LogInformation("Fetching transactions for account {Account ({AccNumber})}, " +
                "Timespan: {startDate} - {endDate}",
                account.Type,
                account.Number,
                startDate, endDate ?? DateTime.Now);

            UpdateConnectionDetails(account);

            var result = await _fintsClient.Transactions(
                new TANDialog(WaitForTanAsync), startDate, endDate);

            HBCIOutput(result.Messages);

            if (!result.IsSuccess)
            {
                _logger?.LogWarning("Fetching transactions was not successful.");

                return new(successful: false);
            }

            _logger?.LogInformation("Fetching transactions was successful.");

            // Parse all non pending transactions
            IEnumerable<MTransaction> transactions = result.Data
                .Where(stmt => !stmt.Pending)
                .SelectMany(stmt => ParseFromSwift(stmt, account));

            return Result.Successful(transactions);
        }

        private static IEnumerable<MTransaction> ParseFromSwift(SwiftStatement swiftStatement, AccountDetails account)
        {
            Dictionary<string, int> uids = new();

            foreach (SwiftTransaction t in swiftStatement.SwiftTransactions)
            {
                MTransaction transaction = Conversion.FromLiveTransaction(t, account);

                if (uids.ContainsKey(transaction.GetUID()))
                {
                    transaction = Conversion.FromLiveTransaction(t, account, ++uids[transaction.GetUID()]);
                }
                else
                {
                    uids.Add(transaction.GetUID(), 0);
                }

                yield return transaction;
            }
        }

        public async Task<Result<decimal>> Balance(AccountDetails account)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            ValidateBankingDetails();

            _logger?.LogInformation("Fetching balance for account {Account ({AccNumber})}...",
                account.Type,
                account.Number);

            UpdateConnectionDetails(account);

            var result = await _fintsClient.Balance(
                new TANDialog(WaitForTanAsync));

            HBCIOutput(result.Messages);

            if (!result.IsSuccess)
            {
                _logger?.LogWarning("Fetching balance was not successful.");

                return new(successful: false);
            }

            _logger?.LogInformation("Fetching balance was successful.");

            return Result.Successful(result.Data.Balance);
        }

        private static async Task<string> WaitForTanAsync(TANDialog tanDialog)
        {
            foreach (var msg in tanDialog.DialogResult.Messages)
                Console.WriteLine(msg);

            return await Task.FromResult(Console.ReadLine());
        }

        /// <summary>
        /// Print HBCI messages
        /// </summary>
        /// <param name="hbciMessages"></param>
        private void HBCIOutput(IEnumerable<HBCIBankMessage> hbciMessages)
        {
            foreach (var msg in hbciMessages)
            {
                _logger?.LogDebug("Code: " + msg.Code + " | " +
                                  "Type: " + msg.Type + " | " +
                                  "Message: " + msg.Message);
            }
        }
    }
}
