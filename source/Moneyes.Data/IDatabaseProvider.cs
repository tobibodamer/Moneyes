using LiteDB;
using System.Threading.Tasks;

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

        Task<bool> TryCreateDatabase();

        Task<bool> TryOpenDatabase();
    }
}