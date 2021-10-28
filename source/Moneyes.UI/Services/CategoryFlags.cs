namespace Moneyes.UI
{
    public enum CategoryFlags
    {
        Real = 1,
        NoCategory = 2,
        AllCategory = 4,
        All = Real | NoCategory | AllCategory,
    }
}