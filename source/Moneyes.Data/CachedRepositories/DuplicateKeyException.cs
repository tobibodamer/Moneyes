using System;

namespace Moneyes.Data
{
    /// <summary>
    /// An exception that is thrown, when a duplicate primary key is encountered during inserts.
    /// </summary>
    public class DuplicateKeyException : Exception
    {
        /// <summary>
        /// The primary key value.
        /// </summary>
#nullable enable
        public object? Key { get; }
#nullable disable

        public DuplicateKeyException(string message, object key = null) : base(message)
        {
            Key = key;
        }
    }
}
