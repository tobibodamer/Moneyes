using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moneyes.Core;
using libfintx;
using Microsoft.Extensions.Logging;
using libfintx.FinTS;
using MTransaction = Moneyes.Core.Transaction;

namespace Moneyes.LiveData
{
    public class OnlineBankingService
    {
        private readonly IFinTsClient _fintsClient;
        private readonly ILogger<OnlineBankingService> _logger;
        private AccountInformation _activeAccount = null;

        internal OnlineBankingService(IFinTsClient client,
            ILogger<OnlineBankingService> logger = null)
        {
            _fintsClient = client;
            _logger = logger;
        }

        public async Task<OnlineBankingResult<AccountInformation>> AccountInformation()
        {
            _logger.LogInformation("Fetching account information");

            var result = await _fintsClient.Accounts(new(WaitForTanAsync));

            HBCIOutput(result.Messages);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Fetching account information was not successful");

                return new(false);
            }

            var activeAccount = result.Data.FirstOrDefault(acc =>
                acc.AccountNumber.Equals(_fintsClient.ConnectionDetails.Account));

            if (activeAccount == null)
            {
                _logger.LogWarning("Account information not found for account {Account ({AccNumber})}...",
                _fintsClient.activeAccount.AccountType,
                _fintsClient.activeAccount.AccountNumber);

                return new(false);
            }

            _logger.LogInformation("Account information found for account {Account ({AccNumber})}...",
                _fintsClient.activeAccount.AccountType,
                _fintsClient.activeAccount.AccountNumber);

            _activeAccount = activeAccount;

            return new(activeAccount);
        }

        public async Task<OnlineBankingResult<IEnumerable<MTransaction>>> Transactions(
            DateTime? startDate = null, DateTime? endDate = null)
        {
            _logger.LogInformation("Fetching transactions for account {Account ({AccNumber})}, " +
                "Timespan: {startDate} - {endDate}",
                _fintsClient.activeAccount.AccountType,
                _fintsClient.activeAccount.AccountNumber,
                startDate, endDate ?? DateTime.Now);

            if (_activeAccount == null)
            {
                _logger.LogInformation("No account information found, fetching account info");

                await AccountInformation();
            }

            var result = await _fintsClient.Transactions(
                new TANDialog(WaitForTanAsync), startDate, endDate);

            HBCIOutput(result.Messages);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Fetching transactions was not successful.");

                return new(false);
            }

            _logger.LogInformation("Fetching transactions was successful.");

            var transactions = result.Data.SelectMany(stmt => stmt.SwiftTransactions)
                .Select(t => Conversion.FromLiveTransaction(t, _activeAccount));

            return new(transactions);
        }

        public async Task<OnlineBankingResult<decimal>> Balance()
        {
            _logger.LogInformation("Fetching balance for account {Account ({AccNumber})}...",
                _fintsClient.activeAccount.AccountType,
                _fintsClient.activeAccount.AccountNumber);

            if (_activeAccount == null)
            {
                _logger.LogInformation("No account information found, fetching account info");

                await AccountInformation();
            }

            var result = await _fintsClient.Balance(
                new TANDialog(WaitForTanAsync));

            HBCIOutput(result.Messages);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Fetching balance was not successful.");

                return new(false);
            }

            _logger.LogInformation("Fetching balance was successful.");

            return new(result.Data.Balance);
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
