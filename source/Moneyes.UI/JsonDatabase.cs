using Moneyes.Core;
using Moneyes.UI.JSON;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Moneyes.UI
{
    public class JsonDatabase<T> : IRepository<T>
    {
        private readonly string _dbFileName;
        private readonly FileSystemWatcher _fileSystemWatcher;
        private Dictionary<object, T> _cache;
        private bool _isUpToDate;
        private readonly bool _isReadOnly;
        private Func<T, object> _primaryKeySelector;

        protected JsonSerializerSettings JsonReadSettings { get; set; } = new();
        protected JsonSerializerSettings JsonWriteSettings { get; set; } = new();

        public event EventHandler<DatabaseUpdatedEventArgs<T>> Updated;

        public JsonDatabase(string fileName, Func<T, object> primaryKeySelector = null, bool isReadOnly = false)
        {
            _dbFileName = Path.GetFullPath(fileName);
            _isReadOnly = isReadOnly;
            _primaryKeySelector = primaryKeySelector ?? (item => item.GetHashCode());

            _fileSystemWatcher = new(
                Path.GetDirectoryName(_dbFileName),
                Path.GetFileName(_dbFileName));

            _fileSystemWatcher.Changed += OnFileChange;
            _fileSystemWatcher.EnableRaisingEvents = true;

            JsonWriteSettings = new()
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        private void OnFileChange(object sender, FileSystemEventArgs e)
        {
            _isUpToDate = false;
        }

        private void UpdateCache(Dictionary<object, T> items)
        {
            _cache = new(items);
            _isUpToDate = true;
        }

        private void ValidateAccess()
        {
            if (!File.Exists(_dbFileName))
            {
                throw new FileNotFoundException("The database file was not found.", _dbFileName);
            }
        }

        /// <summary>
        /// Gets the primary key of the given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private object PK(T item)
        {
            return _primaryKeySelector?.Invoke(item);
        }

        public async Task<T> GetItem(object key)
        {
            if ((await GetAllInternal()).TryGetValue(key, out var value))
            {
                return value;
            }

            return default;
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            return (await GetAllInternal()).Values;
        }

        private async Task<IReadOnlyDictionary<object, T>> GetAllInternal()
        {
            if (!File.Exists(_dbFileName))
            {
                return new Dictionary<object, T>();
            }

            if (_isUpToDate)
            {
                return _cache;
            }

            // deserialize JSON from file
            using FileStream fileStream = File.OpenRead(_dbFileName);
            using StreamReader reader = new(fileStream);

            Dictionary<object, T> items = JsonConvert.DeserializeObject<IEnumerable<T>>(
                await reader.ReadToEndAsync(), JsonReadSettings)
                .ToDictionary(item => PK(item));

            UpdateCache(items);

            return items;
        }

        public async Task SetItem(T item, bool overrideAlways = true)
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException("This database is read only.");
            }

            IReadOnlyDictionary<object, T> existingItems = await GetAllInternal();

            // Add new items
            Dictionary<object, T> itemsToStore = new(existingItems);
            bool changed = false;

            object pk = PK(item);

            if (itemsToStore.ContainsKey(pk))
            {
                if (overrideAlways || !itemsToStore[pk].Equals(item))
                {
                    // Replace
                    itemsToStore[pk] = item;
                    changed = true;
                }
            }
            else
            {
                // Add
                itemsToStore.Add(pk, item);
                changed = true;
            }

            if (changed)
            {
                await SerializeItems(itemsToStore.Values);

                UpdateCache(itemsToStore);
                Updated?.Invoke(this, new(new T[] { item }));
            }
        }

        public async Task SetAll(IEnumerable<T> items, bool overrideAlways = true)
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException("This database is read only.");
            }

            IReadOnlyDictionary<object, T> existingItems = await GetAllInternal();

            // Add new items
            Dictionary<object, T> itemsToStore = new(existingItems);
            List<T> newItems = new();

            foreach (T item in items)
            {
                object pk = PK(item);

                if (itemsToStore.ContainsKey(pk))
                {
                    if (overrideAlways || !itemsToStore[pk].Equals(item))
                    {
                        // Replace
                        itemsToStore[pk] = item;
                        newItems.Add(item);
                    }
                }
                else
                {
                    // Add
                    itemsToStore.Add(pk, item);
                    newItems.Add(item);
                }
            }

            if (newItems.Any())
            {
                // Serialize items

                await SerializeItems(itemsToStore.Values);

                UpdateCache(itemsToStore);
                Updated?.Invoke(this, new(newItems));
            }
        }

        private async Task SerializeItems(IEnumerable<T> items)
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
            try
            {
                using FileStream fileStream = File.Create(_dbFileName);
                using (StreamWriter streamWriter = new(fileStream))
                {

                    string jsonString = JsonConvert.SerializeObject(items, JsonWriteSettings);

                    await streamWriter.WriteAsync(jsonString);
                }
            }
            finally
            {
                _fileSystemWatcher.EnableRaisingEvents = true;
            }
        }
    }
}