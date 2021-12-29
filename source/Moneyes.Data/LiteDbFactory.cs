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
                .Id(c => c.Id, true)
                .DbRef(c => c.Parent, "Category");
            bsonMapper.Entity<AccountDetails>()
                .Id(acc => acc.IBAN, false);
            bsonMapper.Entity<Transaction>()
                .Id(t => t.UID, false)
                .DbRef(t => t.Categories, "Category");
            bsonMapper.Entity<Balance>()     
                .Id(b => b.UID, false)
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
                    serialize: str => SymmetricEncryptor.EncryptString(
                        str.ToUnsecuredString(), password),
                    deserialize: value => value.IsString ? SymmetricEncryptor.DecryptToString(
                        value.AsString, password).ToSecuredString() : null
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