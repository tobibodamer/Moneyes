using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Moneyes.UI
{
    /// <summary>
    /// Provides an interface to retrieve a password asynchronously.
    /// </summary>
    public interface IPasswordPrompt
    {
        Task<(SecureString Password, bool Save)> WaitForPasswordAsync();
    }
}
