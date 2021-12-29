using Microsoft.Extensions.Caching.Memory;
using Moneyes.LiveData;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Moneyes.BankService
{
    public class OnlineBankingSessionStore
    {
        private readonly OnlineBankingServiceFactory _factory = new();
        private readonly MemoryCache _cache = new(new MemoryCacheOptions());

        /// <summary>
        /// Creates a online banking session, or updates an existing session with the given <paramref name="details"/>.
        /// </summary>
        /// <param name="details"></param>
        /// <param name="testConnection"></param>
        /// <returns>The <see cref="IOnlineBankingService"/> of the session.</returns>
        public async Task<IOnlineBankingService> CreateOrUpdateSession(OnlineBankingDetails details, bool testConnection)
        {
            string id = GetId(details);
            bool created = false;

            // Try to get existing session
            if (_cache.TryGetValue(id, out IOnlineBankingService service))
            {
                // Session exists -> update details
                service.BankingDetails.Pin = details.Pin;
            }
            else
            {
                // No session -> create new
                service = _factory.CreateService(details);
                created = true;
            }

            if (testConnection)
            {
                var result = await service.Sync();

                if (!result.IsSuccessful)
                {
                    return null;
                }
            }

            if (!created)
            {
                return service;
            }

            // Save service if created
            _cache.Set(id, service, new MemoryCacheEntryOptions()
            {
                SlidingExpiration = TimeSpan.FromMinutes(30)
            });

            return service;
        }

        /// <summary>
        /// Gets the session from the <paramref name="userId"/> and <paramref name="bankCode"/>,
        /// or <see langword="null"/> if no session exists.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="bankCode"></param>
        /// <returns></returns>
        public IOnlineBankingService GetSession(string userId, string bankCode)
        {
            string id = userId + bankCode;

            if (_cache.TryGetValue(id, out IOnlineBankingService service))
            {
                return service;
            }

            return null;
        }

        private static string GetId(OnlineBankingDetails details)
        {
            return details.UserId + details.BankCode.ToString();
        }
    }
}
