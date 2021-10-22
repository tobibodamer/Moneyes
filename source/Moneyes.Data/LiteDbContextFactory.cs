using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Security;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Options;
using Moneyes.Core;

namespace Moneyes.Data
{
    public class LiteDbContextFactory
    {
        private readonly IOptions<LiteDbConfig> _config;
        public LiteDbContextFactory(IOptions<LiteDbConfig> config)
        {
            _config = config;
        }

        public ILiteDatabase CreateContext(string password = null)
        {
            try
            {
                BsonMapper bsonMapper = new();

                bsonMapper.Entity<Category>()
                    .Id(c => c.Id, true)
                    .DbRef(c => c.Parent);
                bsonMapper.Entity<AccountDetails>()
                    .Id(acc => acc.IBAN, false);
                bsonMapper.Entity<Transaction>()
                    .Id(t => t.UID, false)
                    .DbRef(t => t.Categories);

                if (password != null)
                {
                    bsonMapper.RegisterType<SecureString>
                    (
                        serialize: str => str.ToUnsecuredString(),
                        deserialize: value => value.AsString.ToSecuredString()
                    );
                }

                ConnectionString connectionString = new(_config.Value.DatabasePath)
                {
                    Password = password
                };

                return new LiteDatabase(_config.Value.DatabasePath, bsonMapper);
            }
            catch (Exception ex)
            {
                throw new Exception("Can find or create LiteDb database.", ex);
            }
        }
    }
}
