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
        //public OnlineBankingDetails BankingDetails { get; }

        internal OnlineBankingService(IFinTsClient client,
            ILogger<OnlineBankingService> logger = null)
        {
            _fintsClient = client;
            _logger = logger;            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="account"></param>
        private void UpdateConnectionDetails(OnlineBankingDetails bankingDetails, AccountDetails account = null)
        {
            if (account != null)
            {
                _fintsClient.ConnectionDetails.Account = account.Number;
                _fintsClient.ConnectionDetails.Iban = account.IBAN;
                _fintsClient.ConnectionDetails.AccountHolder = account.OwnerName;
            }

            _fintsClient.ConnectionDetails.Url = bankingDetails.Server.AbsoluteUri;
            _fintsClient.ConnectionDetails.Blz = bankingDetails.BankCode;
            _fintsClient.ConnectionDetails.UserId = bankingDetails.UserId;
            _fintsClient.ConnectionDetails.Pin = bankingDetails.Pin;
        }

        private void ClearConnectionDetails()
        {
            _fintsClient.ConnectionDetails.Account = null;
            _fintsClient.ConnectionDetails.Iban = null;
            _fintsClient.ConnectionDetails.AccountHolder = null;

            _fintsClient.ConnectionDetails.Url = null;
            _fintsClient.ConnectionDetails.Blz = 0;
            _fintsClient.ConnectionDetails.UserId = null;
            _fintsClient.ConnectionDetails.Pin = null;
        }

        private static void ValidateBankingDetails(OnlineBankingDetails bankingDetails)
        {
            ArgumentNullException.ThrowIfNull(bankingDetails, nameof(bankingDetails));

            if (bankingDetails.Pin == null || bankingDetails.Pin.Length == 0)
            {
                throw new ApplicationException("Online banking details must contain valid pin.");
            }
            else if (string.IsNullOrEmpty(bankingDetails.UserId))
            {
                throw new ApplicationException("Online banking details must contain valid user id.");
            }
        }

        /// <summary>
        /// Synchronizes with the online banking API. <br></br>
        /// Can be used to check whether a successful connection can be established to the bank.
        /// </summary>
        /// <returns></returns>
        public async Task<BankingResult> Sync(OnlineBankingDetails onlineBankingDetails)
        {
            ValidateBankingDetails(onlineBankingDetails);
            UpdateConnectionDetails(onlineBankingDetails);

            _logger?.LogInformation("Synchronizing...");

            try
            {
                HBCIDialogResult<string> result = await _fintsClient.Synchronization();

                HBCIOutput(result.Messages);

                if (!result.IsSuccess)
                {
                    _logger?.LogWarning("Synchronizing was not successful");

                    return ParseHBCIError(result.Messages).Item1;
                }

                return BankingResult.Successful();
            }
            finally
            {
                ClearConnectionDetails();
            }
        }

        public async Task<BankingResult<IEnumerable<AccountDetails>>> Accounts(
            OnlineBankingDetails onlineBankingDetails, 
            BankDetails bank)
        {
            ArgumentNullException.ThrowIfNull(bank, nameof(bank));

            ValidateBankingDetails(onlineBankingDetails);
            UpdateConnectionDetails(onlineBankingDetails);

            _logger?.LogInformation("Fetching account information");

            try
            {
                var result = await _fintsClient.Accounts(new(WaitForTanAsync));

                HBCIOutput(result.Messages);

                if (!result.IsSuccess)
                {
                    _logger?.LogWarning("Fetching account information was not successful");

                    return ParseHBCIError(result.Messages).Item1;
                }

                return BankingResult.Successful(result.Data.Select(accInfo =>
                {
                    return new AccountDetails(
                        id: Guid.NewGuid(),
                        number: accInfo.AccountNumber,
                        bankDetails: bank)
                    {
                        IBAN = accInfo.AccountIban,
                        OwnerName = accInfo.AccountOwner,
                        Type = accInfo.AccountType
                    };
                }));
            }
            finally
            {
                ClearConnectionDetails();
            }
        }

        public async Task<BankingResult<TransactionData>> Transactions(
            OnlineBankingDetails onlineBankingDetails,
            AccountDetails account,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            ArgumentNullException.ThrowIfNull(account, nameof(account));

            ValidateBankingDetails(onlineBankingDetails);
            UpdateConnectionDetails(onlineBankingDetails, account);

            _logger?.LogInformation("Fetching transactions for account {Account} ({AccNumber}), " +
                "Timespan: {startDate:dd.MM.yy} - {endDate:dd.MM.yy}",
                account.Type,
                account.Number,
                startDate, endDate ?? DateTime.Now);


            List<SwiftStatement> swiftStatements;

            try
            {
                var result = await _fintsClient.Transactions(
                    new TANDialog(WaitForTanAsync), startDate, endDate);
                
                HBCIOutput(result.Messages);

                if (!result.IsSuccess)
                {
                    _logger?.LogWarning("Fetching transactions was not successful.");

                    return ParseHBCIError(result.Messages).Item1;
                }

                swiftStatements = result.Data;

                _logger?.LogInformation("Fetching transactions was successful.");
            }
            finally
            {
                ClearConnectionDetails();
            }

            try
            {
                // Parse all non pending transactions
                IEnumerable<MTransaction> transactions = swiftStatements
                    .Where(stmt => !stmt.Pending)
                    .SelectMany(stmt => ParseFromSwift(stmt, account));

                IEnumerable<Balance> balances = swiftStatements
                    .Where(stmt => !stmt.Pending)
                    .Select(stmt => new Balance(id: Guid.NewGuid())
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

        public async Task<BankingResult<Balance>> Balance(
            OnlineBankingDetails onlineBankingDetails,
            AccountDetails account)
        {
            ArgumentNullException.ThrowIfNull(account, nameof(account));

            ValidateBankingDetails(onlineBankingDetails);
            UpdateConnectionDetails(onlineBankingDetails, account);

            _logger?.LogInformation("Fetching balance for account {Account} ({AccNumber})}...",
                account.Type,
                account.Number);

            try
            {
                var result = await _fintsClient.Balance(
                    new TANDialog(WaitForTanAsync));

                HBCIOutput(result.Messages);

                if (!result.IsSuccess)
                {
                    _logger?.LogWarning("Fetching balance was not successful.");

                    return ParseHBCIError(result.Messages).Item1;
                }

                _logger?.LogInformation("Fetching balance was successful.");

                return new Balance(id: Guid.NewGuid())
                {
                    Account = account,
                    Date = DateTime.Now,
                    Amount = result.Data.Balance,
                };
            }
            finally
            {
                ClearConnectionDetails();
            }
        }

        private static IEnumerable<MTransaction> ParseFromSwift(SwiftStatement swiftStatement, AccountDetails account)
        {
            Dictionary<string, int> uids = new();

            foreach (SwiftTransaction t in swiftStatement.SwiftTransactions)
            {
                string currency = swiftStatement.Currency;
                MTransaction transaction = Conversion.FromLiveTransaction(t, account, currency);

                if (uids.ContainsKey(transaction.UID))
                {
                    transaction = Conversion.FromLiveTransaction(t, account, currency, ++uids[transaction.UID]);
                }
                else
                {
                    uids.Add(transaction.UID, 0);
                }

                yield return transaction;
            }
        }

        

        private static async Task<string> WaitForTanAsync(TANDialog tanDialog)
        {
            foreach (var msg in tanDialog.DialogResult.Messages)
                Console.WriteLine(msg);

            return await Task.FromResult(Console.ReadLine());
        }

        private static void ThrowFromHBCIError(IEnumerable<HBCIBankMessage> messages)
        {
            var (errorCode, message) = ParseHBCIError(messages);
            throw new OnlineBankingException(errorCode, message);
        }
        private static (OnlineBankingErrorCode, string) ParseHBCIError(IEnumerable<HBCIBankMessage> messages)
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
