using Moneyes.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.UI.Blazor
{
    internal class MauiPasswordPrompt : IPasswordPrompt
    {
        public async Task<(SecureString Password, bool Save)> WaitForPasswordAsync()
        {
            var result = await Application.Current.MainPage.DisplayPromptAsync(
                title: "Password required",
                message: "Enter your online banking password / PIN:");

            return (result?.ToSecuredString(), false);
        }
    }
}
