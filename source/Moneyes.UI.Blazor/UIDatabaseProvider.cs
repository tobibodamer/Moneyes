using Moneyes.Data;
using System.Security;

namespace Moneyes.UI.Blazor
{
    internal class UIDatabaseProvider : LiteDatabaseProvider
    {
        private readonly MasterPasswordProvider _masterPasswordProvider;
        public UIDatabaseProvider(LiteDbConfig dbConfig, MasterPasswordProvider masterPasswordProvider)
            : base(dbConfig)
        {
            _masterPasswordProvider = masterPasswordProvider;
        }

        public override Task<SecureString> OnCreatePassword()
        {
            return _masterPasswordProvider.CreateMasterPassword();
        }

        public override Task<SecureString> OnRequestPassword()
        {
            return _masterPasswordProvider.RequestMasterPassword();
        }
    }
}
