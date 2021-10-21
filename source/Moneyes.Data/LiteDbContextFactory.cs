using System;
using System.Collections;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Options;
using Moneyes.Core;
using Moneyes.LiveData;

namespace Moneyes.Data
{public class LiteDbContextFactory
    {
        private readonly IOptions<LiteDbConfig> _config;
        public LiteDbContextFactory(IOptions<LiteDbConfig> config)
        {
            _config = config;
        }

        public ILiteDatabase CreateContext()
        {
            try
            {
                var bsonMapper = new BsonMapper();

                bsonMapper.Entity<Category>()
                    .Id(c => c.Id, true)
                    .DbRef(c => c.Parent);
                bsonMapper.Entity<AccountDetails>()
                    .Id(acc => acc.IBAN, false);
                bsonMapper.Entity<Transaction>()
                    .Id(t => t.UID, false)
                    .DbRef(t => t.Categories);

                return new LiteDatabase(_config.Value.DatabasePath, bsonMapper);
            }
            catch (Exception ex)
            {
                throw new Exception("Can find or create LiteDb database.", ex);
            }
        }
    }
}
