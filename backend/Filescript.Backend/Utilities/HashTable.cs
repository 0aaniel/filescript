using System;
using System.Collections.Generic;

namespace Filescript.Backend.Utilities {
    /// <summary>
    /// A simple generic hash table implementation using separate chaining for collision resolution.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys.</typeparam>
    /// <typeparam name="TValue">Type of the values.</typeparam>
    public class HashTable<TKey, TValue>
    {
        private readonly int _capacity;
        private readonly List<KeyValuePair<TKey, TValue>>[] _buckets;

        /// <summary>
        /// Initializes a new instance of the <see cref="HashTable{TKey, TValue}"/> class with the specified capacity.
        /// </summary>
        /// <param name="capacity">Number of buckets in the hash table.</param>
        public HashTable(int capacity = 1024)
        {
            _capacity = capacity;
            _buckets = new List<KeyValuePair<TKey, TValue>>[_capacity];
            for (int i = 0; i < _capacity; i++)
            {
                _buckets[i] = new List<KeyValuePair<TKey, TValue>>();
            }
        }

        /// <summary>
        /// Adds a key-value pair to the hash table.
        /// </summary>
        /// <param name="key">Key to add.</param>
        /// <param name="value">Value associated with the key.</param>
        /// <exception cref="ArgumentException">Thrown when the key already exists.</exception>
        public void Add(TKey key, TValue value)
        {
            int bucketIndex = GetBucketIndex(key);
            var bucket = _buckets[bucketIndex];

            foreach (var kvp in bucket)
            {
                if (EqualityComparer<TKey>.Default.Equals(kvp.Key, key))
                {
                    throw new ArgumentException($"An element with the key '{key}' already exists.");
                }
            }

            bucket.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        /// <summary>
        /// Attempts to retrieve the value associated with the specified key.
        /// </summary>
        /// <param name="key">Key to locate.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
        /// <returns>True if the key was found; otherwise, false.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            int bucketIndex = GetBucketIndex(key);
            var bucket = _buckets[bucketIndex];

            foreach (var kvp in bucket)
            {
                if (EqualityComparer<TKey>.Default.Equals(kvp.Key, key))
                {
                    value = kvp.Value;
                    return true;
                }
            }

            value = default(TValue);
            return false;
        }

        /// <summary>
        /// Removes the value with the specified key from the hash table.
        /// </summary>
        /// <param name="key">Key of the element to remove.</param>
        /// <returns>True if the element is successfully found and removed; otherwise, false.</returns>
        public bool Remove(TKey key)
        {
            int bucketIndex = GetBucketIndex(key);
            var bucket = _buckets[bucketIndex];

            for (int i = 0; i < bucket.Count; i++)
            {
                if (EqualityComparer<TKey>.Default.Equals(bucket[i].Key, key))
                {
                    bucket.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the hash table contains the specified key.
        /// </summary>
        /// <param name="key">Key to locate.</param>
        /// <returns>True if the key exists; otherwise, false.</returns>
        public bool ContainsKey(TKey key)
        {
            TValue value;
            return TryGetValue(key, out value);
        }

        /// <summary>
        /// Retrieves all values stored in the hash table.
        /// </summary>
        /// <returns>An enumerable of all values.</returns>
        public IEnumerable<TValue> GetAllValues()
        {
            foreach (var bucket in _buckets)
            {
                foreach (var kvp in bucket)
                {
                    yield return kvp.Value;
                }
            }
        }

        /// <summary>
        /// Calculates the bucket index for the given key.
        /// </summary>
        /// <param name="key">Key to hash.</param>
        /// <returns>Bucket index.</returns>
        private int GetBucketIndex(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            int hashCode = key.GetHashCode();
            // Ensure positive index
            return Math.Abs(hashCode) % _capacity;
        }
    }
}