using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Moneyes.Core
{
    public class CachedTransactionStore : ITransactionStore
    {
        private readonly TransactionStore _store;
        private readonly FileSystemWatcher _fileSystemWatcher;
        private List<Transaction> _cache;
        private bool _isUpToDate;

        public CachedTransactionStore(string fileName)
        {
            _store = new(fileName);
            _fileSystemWatcher = new(
                Path.GetDirectoryName(fileName),
                Path.GetFileName(fileName));

            _fileSystemWatcher.Changed += OnFileChange;
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void OnFileChange(object sender, FileSystemEventArgs e)
        {
            _isUpToDate = false;
        }

        private void UpdateCache(IEnumerable<Transaction> transactions)
        {
            _cache = new(transactions);
            _isUpToDate = true;
        }

        public async Task<IEnumerable<Transaction>> Load()
        {
            if (!_isUpToDate)
            {
                UpdateCache(await _store.Load());
            }

            return _cache;
        }

        public async Task<IEnumerable<Transaction>> Store(IEnumerable<Transaction> transactions)
        {
            _fileSystemWatcher.EnableRaisingEvents = false;

            try
            {
                UpdateCache(await _store.Store(transactions));
            }
            finally
            {
                _fileSystemWatcher.EnableRaisingEvents = true;
            }

            return _cache;
        }
    }


}
