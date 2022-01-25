using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Options;
using Moneyes.Core;

namespace Moneyes.Data
{
    public partial class LiteDbFactory
    {
        private readonly IOptions<LiteDbConfig> _config;
        public LiteDbFactory(IOptions<LiteDbConfig> config)
        {
            _config = config;
        }

        public LiteDbFactory(LiteDbConfig config) : this(Options.Create(config))
        {
        }

        public ILiteDatabase Create(string password = null)
        {
            BsonMapper bsonMapper = new();

            bsonMapper.Entity<Category>()
                .Id(c => c.Id, false)
                .DbRef(c => c.Parent, "Category");
            bsonMapper.Entity<AccountDetails>()
                .Id(acc => acc.Id, false);
            bsonMapper.Entity<Transaction>()
                .Id(t => t.Id, false)
                .DbRef(t => t.Categories, "Category");                
            bsonMapper.Entity<Balance>()
                .Id(b => b.Id, false)
                .Ignore(b => b.IsNegative)
                .DbRef(b => b.Account, "AccountDetails");
            bsonMapper.Entity<BankDetails>()
                .Id(b => b.Id, false);
            
            ConnectionString connectionString = new(_config.Value.DatabasePath);

            if (password == "")
            {
                password = null;
            }

            if (password != null)
            {
                SecureString securePassword = password.ToSecuredString();

                bsonMapper.RegisterType<SecureString>
                (
                    serialize: str => SymmetricEncryptor.EncryptString(
                        str.ToUnsecuredString(), securePassword.ToUnsecuredString()),
                    deserialize: value => value.IsString ? SymmetricEncryptor.DecryptToString(
                        value.AsString, securePassword.ToUnsecuredString()).ToSecuredString() : null
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
    }
}