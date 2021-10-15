using System;
using libfintx.FinTS.Data;
using Microsoft.Extensions.Logging;
using libfintx.FinTS;

namespace Moneyes.LiveData
{
    /// <summary>
    /// Factory to create a <see cref="OnlineBankingService"/>.
    /// </summary>
    public class OnlineBankingServiceFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public OnlineBankingServiceFactory(ILoggerFactory loggerFactory = null)
        {
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Create a <see cref="OnlineBankingService"/> using the given <see cref="OnlineBankingDetails"/>.
        /// </summary>
        /// <param name="bankingDetails">The online banking details.</param>
        /// <returns></returns>
        public OnlineBankingService CreateService(OnlineBankingDetails bankingDetails)
        {
            VerifyBankingDetails(bankingDetails);

            FinTsInstitute institute = FinTsInstitutes.GetInstitute(bankingDetails.BankCode);

            VerifyInstitute(institute);

            var details = new ConnectionDetails()
            {
                Url = institute.FinTs_Url,
                Blz = bankingDetails.BankCode,
                Account = bankingDetails.AccountNumber,
                UserId = bankingDetails.UserId,
                Pin = bankingDetails.Pin
            };

            FinTsClient client = new(details);

            return new(client, _loggerFactory?.CreateLogger<OnlineBankingService>());
        }

        private static void VerifyBankingDetails(OnlineBankingDetails bankingDetails)
        {
            if (bankingDetails == null)
            {
                throw new ArgumentNullException(nameof(bankingDetails));
            }
            else if (string.IsNullOrEmpty(bankingDetails.Pin))
            {
                throw new ArgumentException("Online banking details must contain valid pin.");
            }
            else if (string.IsNullOrEmpty(bankingDetails.UserId))
            {
                throw new ArgumentException("Online banking details must contain valid user id.");
            }
            else if (string.IsNullOrEmpty(bankingDetails.AccountNumber))
            {
                throw new ArgumentException("Online banking details must contain valid account number.");
            }
            else if (bankingDetails.BankCode == 0)
            {
                throw new ArgumentException("Online banking details must contain valid bank code.");
            }
        }

        private static void VerifyInstitute(FinTsInstitute institute)
        {
            if (institute is null)
            {
                throw new NotSupportedException();
            }

            if (string.IsNullOrEmpty(institute.FinTs_Url))
            {
                throw new NotSupportedException();
            }

            if (!Uri.TryCreate(institute.FinTs_Url, UriKind.Absolute, out _))
            {
                throw new NotSupportedException();
            }
        }
    }
}
