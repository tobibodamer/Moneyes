using System;
using System.IO;
using System.Security;
using LiteDB;
using Moneyes.Core;

namespace Moneyes.Data
{
    public class DatabaseProvider : IDatabaseProvider
    {
        private Func<SecureString?> _createMasterPasswordFunc;
        private Func<SecureString> _requestMasterPasswordFunc;

        private LiteDbConfig _dbConfig;
        //private Lazy<ILiteDatabase> _databaseLazy;
        private ILiteDatabase _database;
        public ILiteDatabase Database => _database;

        public bool IsDatabaseCreated => File.Exists(_dbConfig.DatabasePath);
        public bool IsOpen => Database != null;

        public DatabaseProvider(
            Func<SecureString> createMasterPasswordFunc,
            Func<SecureString> requestMasterPasswordFunc,
            LiteDbConfig dbConfig)
        {
            _createMasterPasswordFunc = createMasterPasswordFunc;
            _requestMasterPasswordFunc = requestMasterPasswordFunc;
            _dbConfig = dbConfig;
        }

        public bool TryCreateDatabase()
        {
            if (IsDatabaseCreated)
            {
                throw new Exception("Already created");
            }

            try
            {
                LiteDbContextFactory databaseFactory = new(_dbConfig);

                SecureString newPassword = _createMasterPasswordFunc?.Invoke();

                if (newPassword is null)
                {
                    return false;
                }

                ILiteDatabase database = databaseFactory
                    .CreateContext(newPassword.ToUnsecuredString());

                _database = database;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryOpenDatabase()
        {
            if (!IsDatabaseCreated)
            {
                return false;
            }

            LiteDbContextFactory databaseFactory = new(_dbConfig);

            try
            {
                _database = databaseFactory.CreateContext();

                return true;
            }
            catch (LiteException ex)
            {
                // Failed to open without master password
            }

            SecureString password;

            // No password failed, or database doesn't exist -> Try with master password
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    password = _requestMasterPasswordFunc?.Invoke();

                    if (password is null)
                    {
                        // Return directly, null means no password supplied, but expected
                        return false;
                    }

                    _database = databaseFactory
                        .CreateContext(password.ToUnsecuredString());

                    return true;
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message, "Could not open database", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Opening database failed
            return false;
        }
    }
}