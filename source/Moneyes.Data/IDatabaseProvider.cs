using LiteDB;

namespace Moneyes.Data
{
    public interface IDatabaseProvider
    {
        ILiteDatabase Database { get; }

        bool IsDatabaseCreated { get; }
        bool IsOpen { get; }

        bool TryCreateDatabase();

        bool TryOpenDatabase();
    }
}