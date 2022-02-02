using System;

namespace Moneyes.Data
{
    [Flags]
    public enum RepositoryChangedAction
    {
        Add = 1,
        Replace = 2,
        Remove = 4
    }
}
