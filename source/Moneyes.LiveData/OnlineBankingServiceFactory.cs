using System;
using libfintx.FinTS.Data;
using Microsoft.Extensions.Logging;
using libfintx.FinTS;

namespace Moneyes.LiveData
{
    /// <summary>
    /// Factory to create a <see cref="OnlineBankingService"/>.
    /// </summary>
    public class OnlineBankingServiceFactory : IOnlineBankingServiceFactory
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
        public IOnlineBankingService CreateService(OnlineBankingDetails bankingDetails)
        {
            ValidateBankingDetails(bankingDetails);

            FinTsInstitute institute = BankInstitutes.GetInstituteInternal(bankingDetails.BankCode);

            ValidateInstitute(institute);

            ConnectionDetails details = new()
            {
                Url = institute.FinTs_Url,
                Blz = bankingDetails.BankCode,
                UserId = bankingDetails.UserId,
                Pin = bankingDetails.Pin
            };

            FinTsClient client = new(details);

            return new OnlineBankingService(
                client, bankingDetails, _loggerFactory?.CreateLogger<OnlineBankingService>());
        }

        private static void ValidateBankingDetails(OnlineBankingDetails bankingDetails)
        {
            if (bankingDetails == null)
            {
                throw new ArgumentNullException(nameof(bankingDetails));
            }
            else if (string.IsNullOrEmpty(bankingDetails.UserId))
            {
                throw new ArgumentException("Online banking details must contain valid user id.");
            }
            else if (bankingDetails.BankCode.ToString().Length != 8)
            {
                throw new ArgumentException("Online banking details must contain valid bank code.");
            }
        }

        private static void ValidateInstitute(FinTsInstitute institute)
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
