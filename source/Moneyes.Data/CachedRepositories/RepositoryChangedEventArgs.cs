using System;
using System.Collections.Generic;

namespace Moneyes.Data
{
    public class RepositoryChangedEventArgs<T> : EventArgs
    {
        public IReadOnlyList<T> AddedItems { get; init; }
        public IReadOnlyList<T> ReplacedItems { get; init; }
        public IReadOnlyList<T> RemovedItems { get; init; }
        public RepositoryChangedAction Actions { get; }

        public RepositoryChangedEventArgs(RepositoryChangedAction actions)
        {
            Actions = actions;
        }
    }
}
