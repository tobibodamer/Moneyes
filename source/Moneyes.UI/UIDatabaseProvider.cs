using Moneyes.Data;
using System.Security;

namespace Moneyes.UI
{
    public partial class App
    {
        internal class UIDatabaseProvider : LiteDatabaseProvider
        {
            private readonly MasterPasswordProvider _masterPasswordProvider;
            public UIDatabaseProvider(LiteDbConfig dbConfig, MasterPasswordProvider masterPasswordProvider)
                : base(dbConfig)
            {
                _masterPasswordProvider = masterPasswordProvider;
            }

            public override SecureString OnCreatePassword()
            {
                return _masterPasswordProvider.CreateMasterPassword();
            }

            public override SecureString OnRequestPassword()
            {
                return _masterPasswordProvider.RequestMasterPassword();
            }
        }
    }
}
