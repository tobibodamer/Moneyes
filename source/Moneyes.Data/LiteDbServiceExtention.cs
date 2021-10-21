using LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace Moneyes.Data
{
    public static class LiteDbServiceExtention
    {
        public static void AddLiteDb(this IServiceCollection services, string databasePath)
        {
            services.AddScoped<LiteDbContextFactory>();
            services.Configure<LiteDbConfig>(options => options.DatabasePath = databasePath);
            services.AddSingleton<ILiteDatabase>(p => p.GetRequiredService<LiteDbContextFactory>().CreateContext());
        }
    }
}
