using System;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using LiteDB;
using Moneyes.Core;

namespace Moneyes.Data
{
    public class LiteDatabaseProvider : IDatabaseProvider<ILiteDatabase>
    {
        private Func<SecureString> _createMasterPasswordFunc;
        private Func<SecureString> _requestMasterPasswordFunc;

        private LiteDbConfig _dbConfig;
        //private Lazy<ILiteDatabase> _databaseLazy;
        private ILiteDatabase _database;
        public ILiteDatabase Database => _database;

        public bool IsDatabaseCreated => File.Exists(_dbConfig.DatabasePath);
        public bool IsOpen => Database != null;

        public LiteDatabaseProvider(
            LiteDbConfig dbConfig)
        {
            _createMasterPasswordFunc = dbConfig.CreatePassword;
            _requestMasterPasswordFunc = dbConfig.RequestPassword;
            _dbConfig = dbConfig;
        }

#nullable enable
        public virtual Task<SecureString>? OnCreatePassword()
#nullable disable
        {
            return Task.FromResult(_createMasterPasswordFunc());
        }
        public virtual Task<SecureString> OnRequestPassword()
        {
            return Task.FromResult(_requestMasterPasswordFunc());
        }

        public async Task<bool> TryCreateDatabase()
        {
            if (IsDatabaseCreated)
            {
                throw new Exception("Already created");
            }

            try
            {
                LiteDbFactory databaseFactory = new(_dbConfig);

                SecureString newPassword = await OnCreatePassword();

                if (newPassword is null)
                {
                    return false;
                }

                ILiteDatabase database = databaseFactory
                    .Create(newPassword.ToUnsecuredString());

                _database = database;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TryOpenDatabase()
        {
            if (!IsDatabaseCreated)
            {
                return false;
            }

            LiteDbFactory databaseFactory = new(_dbConfig);

            try
            {
                _database = databaseFactory.Create();

                return true;
            }
            catch (LiteException)
            {
                // Failed to open without master password
            }

            SecureString password;

            // No password failed, or database doesn't exist -> Try with master password
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    password = await OnRequestPassword();

                    if (password is null)
                    {
                        // Return directly, null means no password supplied, but expected
                        return false;
                    }

                    _database = databaseFactory
                        .Create(password.ToUnsecuredString());

                    return true;
                }
                catch
                {
                    //MessageBox.Show(ex.Message, "Could not open database", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Opening database failed
            return false;
        }
    }
}