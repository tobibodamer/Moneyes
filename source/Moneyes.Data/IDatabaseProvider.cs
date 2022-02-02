using LiteDB;

namespace Moneyes.Data
{
    /// <summary>
    /// Provides
    /// </summary>
    public interface IDatabaseProvider<T>
    {
        T Database { get; }

        bool IsDatabaseCreated { get; }
        bool IsOpen { get; }

        bool TryCreateDatabase();

        bool TryOpenDatabase();
    }
}