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
    public class LiteDbFactory
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
            BsonMapper bsonMapper = _config.Value.BsonMapper ?? new();

            ConnectionString connectionString = new(_config.Value.DatabasePath);

            if (password == "")
            {
                password = null;
            }

            if (password != null)
            {
                if (_config.Value.EncryptSecureStrings)
                {
                    SecureString securePassword = password.ToSecuredString();
                    
                    bsonMapper.RegisterType<SecureString>
                    (
                        serialize: str => SymmetricEncryptor.EncryptString(
                            str.ToUnsecuredString(), securePassword.ToUnsecuredString()),
                        deserialize: value => value.IsString ? SymmetricEncryptor.DecryptToString(
                            value.AsString, securePassword.ToUnsecuredString()).ToSecuredString() : null
                    );
                }
                connectionString.Password = password;
            }
            else
            {
                if (_config.Value.EncryptSecureStrings)
                {
                    bsonMapper.RegisterType<SecureString>
                    (
                        serialize: str => EncryptionMethods.EncryptString(str.ToUnsecuredString()),
                        deserialize: value => value.IsString ?
                            EncryptionMethods.DecryptString(value.AsString).ToSecuredString() : null
                    );
                }
            }

            return new LiteDatabase(connectionString, bsonMapper);
        }
    }
}