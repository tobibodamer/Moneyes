using LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace Moneyes.Data
{
    public class LiteDbBuilder
    {
        public IServiceCollection Services { get; }

        public LiteDbBuilder(IServiceCollection services)
        {
            Services = services;

            services.AddScoped<LiteDbFactory>(p =>
            {
                var config = p.GetRequiredService<LiteDbConfig>();
                return new(config);
            });
        }
    }
}
