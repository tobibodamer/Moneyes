using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.Data
{
    internal class UniqueIndex<T, TKey> : IUniqueIndex<T, TKey>
    {
        private readonly Dictionary<int, TKey> _hashEntityMap = new();
        private readonly Dictionary<TKey, int> _keyHashMap = new();

        
        public IUniqueConstraint<T> Constraint { get; }

        /// <summary>
        /// Creates an empty index with the given constraint.
        /// </summary>
        /// <param name="uniqueConstraint"></param>
        public UniqueIndex(IUniqueConstraint<T> uniqueConstraint)
        {
            Constraint = uniqueConstraint;
        }

        /// <summary>
        /// Creates an index with the given constraint and adds the entities, given by key - entity pairs.
        /// </summary>
        /// <param name="uniqueConstraint"></param>
        /// <param name="entities"></param>
        public UniqueIndex(IUniqueConstraint<T> uniqueConstraint, IEnumerable<KeyValuePair<TKey, T>> entities)
            : this(uniqueConstraint)
        {
            foreach (var kvp in entities)
            {
                var key = kvp.Key;
                var entity = kvp.Value;

                var hash = Constraint.HashPropertyValue(entity);

                if (hash is null)
                {
                    if (Constraint.NullValueHandling is NullValueHandling.Ignore)
                    {
                        continue;
                    }
                    else if (Constraint.NullValueHandling is NullValueHandling.Include)
                    {
                        hash = 0;
                    }
                }

                _hashEntityMap[hash.Value] = key;
                _keyHashMap[key] = hash.Value;
            }
        }

        /// <summary>
        /// Creates a new index by copying an existing index.
        /// </summary>
        /// <param name="other"></param>
        protected UniqueIndex(UniqueIndex<T, TKey> other)
        {
            _hashEntityMap = new Dictionary<int, TKey>(other._hashEntityMap);
            _keyHashMap = new Dictionary<TKey, int>(other._keyHashMap);
            Constraint = other.Constraint;
        }

        public IUniqueIndex<T, TKey> Copy()
        {
            return new UniqueIndex<T, TKey>(this);
        }

        /// <summary>
        /// Creates a hash for the unique property of this index.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="uniqueHash"></param>
        /// <returns><see langword="true"/> if the hash was created, 
        /// or <see langword="false"/> if the unique property is null and therefore has no hash.</returns>
        public bool CreateHash(T entity, out int uniqueHash)
        {
            var hash = Constraint.HashPropertyValue(entity);

            uniqueHash = hash ?? 0;

            if (hash is null)
            {
                if (Constraint.NullValueHandling is NullValueHandling.Ignore)
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryGetExistingEntity(T entity, out TKey existingKey)
        {
            if (!CreateHash(entity, out var hash))
            {
                existingKey = default;
                return false;
            }

            return _hashEntityMap.TryGetValue(hash, out existingKey);
        }
        public bool TryGetExistingEntity(int uniqueHash, out TKey existingKey)
        {
            return _hashEntityMap.TryGetValue(uniqueHash, out existingKey);
        }

        public bool GetOrAddEntity(TKey key, T entity, out TKey existingKey)
        {
            if (!CreateHash(entity, out var hash))
            {
                existingKey = default;
                return true;
            }

            return GetOrAddEntity(key, hash, out existingKey);
        }
        public bool GetOrAddEntity(TKey key, int uniqueHash, out TKey existingKey)
        {
            if (_hashEntityMap.TryGetValue(uniqueHash, out existingKey))
            {
                return false;
            }

            _hashEntityMap.Add(uniqueHash, key);

            if (!_keyHashMap.TryAdd(key, uniqueHash))
            {
                _hashEntityMap.Remove(uniqueHash);
                existingKey = key;
                return false;
            }

            existingKey = default;
            return true;
        }

        public void UpdateEntity(TKey key, T entity)
        {
            if (!CreateHash(entity, out var hash))
            {
                return;
            }

            UpdateEntity(key, hash);
        }
        public void UpdateEntity(TKey key, int uniqueHash)
        {
            if (_keyHashMap.TryGetValue(key, out var oldHash))
            {
                if (uniqueHash == oldHash)
                {
                    return;
                }

                _hashEntityMap.Remove(oldHash);
            }

            _hashEntityMap[uniqueHash] = key;
            _keyHashMap[key] = uniqueHash;
        }

        public void RemoveEntity(TKey key)
        {
            // If entity is indexed -> remove index and entity
            if (_keyHashMap.Remove(key, out var oldHash))
            {
                _hashEntityMap.Remove(oldHash);
            }
        }
    }
}
