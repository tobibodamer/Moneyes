using System;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace Moneyes.Data
{
    [SupportedOSPlatform("windows")]
    static class EncryptionMethods
    {
        public static string EncryptString(string str)
        {
            try
            {
                var bytes = Encoding.Default.GetBytes(str);

                return Convert.ToBase64String(ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser));
            }
            catch
            {
                return null;
            }
        }

        public static string DecryptString(string str)
        {
            try
            {
                var bytes = Convert.FromBase64String(str);

                return Encoding.Default.GetString(ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser));
            }
            catch
            {
                return null;
            }
        }
    }
}