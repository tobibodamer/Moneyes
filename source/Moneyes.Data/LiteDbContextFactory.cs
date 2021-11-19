using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security;
using System.Security.Cryptography;
using System.Text;
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

        public LiteDbContextFactory(LiteDbConfig config) : this(Options.Create(config))
        {
        }

        public ILiteDatabase CreateContext(string password = null)
        {
            try
            {
                BsonMapper bsonMapper = new();

                bsonMapper.Entity<Category>()
                    .Id(c => c.Id, true)
                    .DbRef(c => c.Parent, "Category");
                bsonMapper.Entity<AccountDetails>()
                    .Id(acc => acc.IBAN, false);
                bsonMapper.Entity<Transaction>()
                    .Id(t => t.UID, false)
                    .DbRef(t => t.Categories, "Category");
                bsonMapper.Entity<Balance>()
                    .Ignore(b => b.IsNegative)
                    .DbRef(b => b.Account, "AccountDetails");

                ConnectionString connectionString = new(_config.Value.DatabasePath);

                if (password == "")
                {
                    password = null;
                }

                if (password != null)
                {
                    bsonMapper.RegisterType<SecureString>
                    (
                        serialize: str => str.ToUnsecuredString(),
                        deserialize: value => value.IsString ? value.AsString.ToSecuredString() : null
                    );

                    connectionString.Password = password;
                }
                else
                {
                    bsonMapper.RegisterType<SecureString>
                        (
                            serialize: str => EncryptionMethods.EncryptString(str.ToUnsecuredString()),
                            deserialize: value => value.IsString ?
                                EncryptionMethods.DecryptString(value.AsString).ToSecuredString() : null
                        );
                }

                return new LiteDatabase(connectionString, bsonMapper);
            }
            catch
            {
                //throw new Exception("Can find or create LiteDb database.", ex);
                throw;
            }
        }

        static class EncryptionMethods
        {
            public static string EncryptString(string str)
            {
                try
                {
                    var bytes = Encoding.Default.GetBytes(str);

                    return Convert.ToBase64String(ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser));
                }
                catch
                {
                    return null;
                }
            }

            public static string DecryptString(string str)
            {
                try
                {
                    var bytes = Convert.FromBase64String(str);

                    return Encoding.Default.GetString(ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser));
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}