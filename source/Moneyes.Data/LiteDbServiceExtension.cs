using LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace Moneyes.Data
{
    public static class LiteDbServiceExtension
    {
        public static void AddLiteDb(this IServiceCollection services, string databasePath)
        {
            services.AddScoped<LiteDbFactory>();
            services.Configure<LiteDbConfig>(options => options.DatabasePath = databasePath);
        }
    }
}
