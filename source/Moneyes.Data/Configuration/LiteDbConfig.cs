using LiteDB;
using System;
using System.Security;

namespace Moneyes.Data
{
    public class LiteDbConfig
    {
        public string DatabasePath { get; set; }

        /// <summary>
        /// Gets or sets a delegate that provides a password as <see cref="SecureString"/> 
        /// when creating the database.
        /// </summary>
#nullable enable
        public Func<SecureString?>? CreatePassword { get; set; }
#nullable disable

        /// <summary>
        /// Gets or sets a delegate that provides a password as <see cref="SecureString"/> 
        /// when opening a protected database.
        /// </summary>
#nullable enable
        public Func<SecureString>? RequestPassword { get; set; }
#nullable disable

#nullable enable
        public BsonMapper? BsonMapper { get; set; }
#nullable disable

        public bool EncryptSecureStrings { get; set; } = true;
    }
}
