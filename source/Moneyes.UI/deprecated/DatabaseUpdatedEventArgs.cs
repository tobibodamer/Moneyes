using System;
using System.Collections.Generic;

namespace Moneyes.UI
{
    public class DatabaseUpdatedEventArgs<T> : EventArgs
    {
        public IEnumerable<T> Items { get; }

        public DatabaseUpdatedEventArgs(IEnumerable<T> items)
        {
            Items = items;
        }
    }
}