using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace Moneyes.Core
{
    public class TransactionStore : ITransactionStore
    {
        private readonly string _dbFileName;

        public TransactionStore(string fileName)
        {
            _dbFileName = fileName;
        }

        public async Task<IEnumerable<Transaction>> Load()
        {
            if (!File.Exists(_dbFileName))
            {
                return Enumerable.Empty<Transaction>();
            }

            try
            {
                // deserialize JSON from file
                using FileStream fileStream = File.OpenRead(_dbFileName);

                return await JsonSerializer.DeserializeAsync<IEnumerable<Transaction>>(fileStream,
                    new JsonSerializerOptions()
                    {
                    });
            }
            catch
            {
                return Enumerable.Empty<Transaction>();
            }
        }

        public async Task<IEnumerable<Transaction>> Store(IEnumerable<Transaction> transactions)
        {
            IEnumerable<Transaction> existingTransactions = await Load();

            var updatedTransactions = existingTransactions
                .Union(transactions)
                .OrderBy(t => t.BookingDate);

            // serialize JSON directly to a file
            using FileStream fileStream = File.Create(_dbFileName);
            using StreamWriter streamWriter = new(fileStream);

            await JsonSerializer.SerializeAsync<IEnumerable<Transaction>>(fileStream, updatedTransactions,
                new JsonSerializerOptions()
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                });

            return updatedTransactions;
        }
    }


}
