using Moneyes.Data;
using System.Security;
using System.Threading.Tasks;

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

            public override Task<SecureString> OnCreatePassword()
            {
                return Task.FromResult(_masterPasswordProvider.CreateMasterPassword());
            }

            public override Task<SecureString> OnRequestPassword()
            {
                return Task.FromResult(_masterPasswordProvider.RequestMasterPassword());
            }
        }
    }
}
