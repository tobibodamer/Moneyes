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
    public class OnlineBankingService : IOnlineBankingService
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

            if (BankingDetails.Server != null)
            {
                _fintsClient.ConnectionDetails.Url = BankingDetails.Server.AbsoluteUri;
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
        public async Task<BankingResult> Sync()
        {
            ValidateBankingDetails();

            _logger?.LogInformation("Synchronizing...");

            UpdateConnectionDetails();

            HBCIDialogResult<string> result = await _fintsClient.Synchronization();

            HBCIOutput(result.Messages);

            if (!result.IsSuccess)
            {
                _logger?.LogWarning("Synchronizing was not successful");

                return ParseHBCIError(result.Messages).Item1;
            }

            return BankingResult.Successful();
        }

        static void ThrowFromHBCIError(IEnumerable<HBCIBankMessage> messages)
        {
            var (errorCode, message) = ParseHBCIError(messages);
            throw new OnlineBankingException(errorCode, message);
        }
        static (OnlineBankingErrorCode, string) ParseHBCIError(IEnumerable<HBCIBankMessage> messages)
        {
            foreach (var msg in messages.Where(msg => msg.IsError))
            {
                //if (int.TryParse(msg.Code, out var code))
                //{
                //    return ((OnlineBankingErrorCode)code, msg.Message);
                //}
                // See codes: https://wiki.windata.de/index.php?title=HBCI-Fehlermeldungen

                switch (msg.Code)
                {
                    case "9931":
                        return (OnlineBankingErrorCode.InvalidUsernameOrPin, msg.Message);
                    case "9942":
                        return (OnlineBankingErrorCode.InvalidPin, msg.Message);
                }
            }

            return (OnlineBankingErrorCode.Unknown, null);

        }

        public async Task<BankingResult<IEnumerable<AccountDetails>>> Accounts()
        {
            ValidateBankingDetails();

            _logger?.LogInformation("Fetching account information");

            UpdateConnectionDetails();

            var result = await _fintsClient.Accounts(new(WaitForTanAsync));

            HBCIOutput(result.Messages);

            if (!result.IsSuccess)
            {
                _logger?.LogWarning("Fetching account information was not successful");

                return ParseHBCIError(result.Messages).Item1;
            }

            return BankingResult.Successful(result.Data.Select(accInfo =>
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

        public async Task<BankingResult<TransactionData>> Transactions(
            AccountDetails account,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            ValidateBankingDetails();

            _logger?.LogInformation("Fetching transactions for account {Account} ({AccNumber}), " +
                "Timespan: {startDate:dd.MM.yy} - {endDate:dd.MM.yy}",
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

                return ParseHBCIError(result.Messages).Item1;
            }

            _logger?.LogInformation("Fetching transactions was successful.");

            try
            {

                // Parse all non pending transactions
                IEnumerable<MTransaction> transactions = result.Data
                    .Where(stmt => !stmt.Pending)
                    .SelectMany(stmt => ParseFromSwift(stmt, account));

                IEnumerable<Balance> balances = result.Data
                    .Where(stmt => !stmt.Pending)
                    .Select(stmt => new Balance
                    {
                        Account = account,
                        Amount = stmt.EndBalance,
                        Date = stmt.EndDate,
                        Currency = stmt.Currency
                    });

                TransactionData transactionData = new()
                {
                    Transactions = transactions,
                    Balances = balances
                };

                return transactionData;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error while parsing transactions or balances");
                return BankingResult.Failed<TransactionData>();
            }
        }

        private static IEnumerable<MTransaction> ParseFromSwift(SwiftStatement swiftStatement, AccountDetails account)
        {
            Dictionary<string, int> uids = new();

            foreach (SwiftTransaction t in swiftStatement.SwiftTransactions)
            {
                string currency = swiftStatement.Currency;
                MTransaction transaction = Conversion.FromLiveTransaction(t, account, currency);

                if (uids.ContainsKey(transaction.GetUID()))
                {
                    transaction = Conversion.FromLiveTransaction(t, account, currency, ++uids[transaction.GetUID()]);
                }
                else
                {
                    uids.Add(transaction.GetUID(), 0);
                }

                yield return transaction;
            }
        }

        public async Task<BankingResult<Balance>> Balance(AccountDetails account)
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

                return ParseHBCIError(result.Messages).Item1;
            }

            _logger?.LogInformation("Fetching balance was successful.");

            return new Balance()
            {
                Account = account,
                Date = DateTime.Now,
                Amount = result.Data.Balance
            };
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

    public class TransactionData
    {
        public IEnumerable<MTransaction> Transactions { get; init; }
        public IEnumerable<Balance> Balances { get; init; }
    }
}
