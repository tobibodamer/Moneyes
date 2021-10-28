using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.UI
{
    static class Extensions
    {
        public static bool IsNullOrEmpty(this SecureString secureString)
        {
            return secureString == null || secureString.Length == 0;
        }
    }
}
