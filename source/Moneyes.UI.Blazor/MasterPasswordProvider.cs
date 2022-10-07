using Moneyes.Core;
using System;
using System.Security;

namespace Moneyes.UI.Blazor
{
    internal class MasterPasswordProvider
    {
        public SecureString CachedMasterPassword { get; private set; }

        /// <summary>
        /// Creates a new master password.
        /// </summary>
        /// <returns>Returns the new master password, 
        /// or <see langword="null"/> if not password returned.</returns>
        public async Task<SecureString> CreateMasterPassword()
        {
            var result = await Application.Current.MainPage.DisplayPromptAsync(
                title: "Master password required",
                message: "This is the first start of the application. \r \n" +
                         "Please select a master password to keep your personal data secure.&#10;You will be asked for this password at each start of the application.",
                cancel: "Skip");

            if (result != null)
            {
                return CachedMasterPassword = result.ToSecuredString();
            }
            else
            {
                CachedMasterPassword = null;

                return string.Empty.ToSecuredString();
            }
        }

        /// <summary>
        /// Request the master password.
        /// </summary>
        /// <returns>Returns the master password or <see langword="null"/> if not password returned.</returns>
        public async Task<SecureString> RequestMasterPassword()
        {
            var result = await Application.Current.MainPage.DisplayPromptAsync(
                title: "Master password required",
                message: "Please enter your master password:");

            if (result != null)
            {
                return CachedMasterPassword = result.ToSecuredString();
            }

            return null;
        }
    }
}
