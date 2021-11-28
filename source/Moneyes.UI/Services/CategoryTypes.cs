using System;

namespace Moneyes.UI
{
    [Flags]
    public enum CategoryTypes
    {
        Real = 1,
        NoCategory = 2,
        AllCategory = 4,
        All = Real | NoCategory | AllCategory,
    }
}