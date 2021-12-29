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

        public IOnlineBankingService CreateService()
        {
            ConnectionDetails details = new();

            FinTsClient client = new(details);

            return new OnlineBankingService(
                client, new OnlineBankingDetails(), _loggerFactory?.CreateLogger<OnlineBankingService>());
        }

        /// <summary>
        /// Create a <see cref="OnlineBankingService"/> using the given <see cref="OnlineBankingDetails"/>.
        /// </summary>
        /// <param name="bankingDetails">The online banking details.</param>
        /// <returns></returns>
        public IOnlineBankingService CreateService(OnlineBankingDetails bankingDetails)
        {
            OnlineBankingDetails bankingDetailsCopy = bankingDetails.Copy();

            ValidateBankingDetails(bankingDetailsCopy);

            if (bankingDetailsCopy.Server is null)
            {
                FinTsInstitute institute = BankInstitutes.GetInstituteInternal(bankingDetailsCopy.BankCode);

                ValidateInstitute(institute);

                bankingDetailsCopy = new()
                {
                    BankCode = bankingDetails.BankCode,
                    UserId = bankingDetails.UserId,
                    Pin = bankingDetails.Pin,
                    Server = new(institute.FinTs_Url)
                };
            }

            ConnectionDetails details = new()
            {
                Url = bankingDetailsCopy.Server.AbsoluteUri,
                Blz = bankingDetailsCopy.BankCode,
                UserId = bankingDetailsCopy.UserId,
                Pin = bankingDetailsCopy.Pin,
            };

            FinTsClient client = new(details);

            return new OnlineBankingService(
                client, bankingDetailsCopy, _loggerFactory?.CreateLogger<OnlineBankingService>());
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
