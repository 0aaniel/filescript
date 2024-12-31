using System;
using System.Collections.Generic;
using Filescript.Backend.Utilities;

namespace Filescript.Backend.DataStructures.HashTable
{
    /// <summary>
    /// Represents a simple hash table implementation.
    /// </summary>
    /// <typeparam name="TKey">Type of keys.</typeparam>
    /// <typeparam name="TValue">Type of values.</typeparam>
    public class HashTable<TKey, TValue>
    {
        private readonly int _numberOfBuckets;
        private readonly LinkedList<HashTableNode<TKey, TValue>>[] _buckets;
        private readonly HashFunction<TKey> _hashFunction;

        /// <summary>
        /// Initializes a new instance of the <see cref="HashTable{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="numberOfBuckets">The number of buckets to use.</param>
        public HashTable(int numberOfBuckets = 1024)
        {
            if (numberOfBuckets <= 0)
                throw new ArgumentException("Number of buckets must be positive.", nameof(numberOfBuckets));

            _numberOfBuckets = numberOfBuckets;
            _buckets = new LinkedList<HashTableNode<TKey, TValue>>[_numberOfBuckets];
            for (int i = 0; i < _numberOfBuckets; i++)
            {
                _buckets[i] = new LinkedList<HashTableNode<TKey, TValue>>();
            }
            _hashFunction = new HashFunction<TKey>();
        }

        /// <summary>
        /// Adds a key-value pair to the hash table.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Add(TKey key, TValue value)
        {
            int bucketIndex = _hashFunction.ComputeHash(key, _numberOfBuckets);
            var bucket = _buckets[bucketIndex];

            foreach (var node in bucket)
            {
                if (node.Key.Equals(key))
                    throw new ArgumentException($"An element with the key '{key}' already exists.");
            }

            var newNode = new HashTableNode<TKey, TValue>(key, value);
            bucket.AddLast(newNode);
        }

        /// <summary>
        /// Tries to get the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if found.</param>
        /// <returns>True if the key was found; otherwise, false.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            int bucketIndex = _hashFunction.ComputeHash(key, _numberOfBuckets);
            var bucket = _buckets[bucketIndex];

            foreach (var node in bucket)
            {
                if (node.Key.Equals(key))
                {
                    value = node.Value;
                    return true;
                }
            }

            value = default(TValue);
            return false;
        }

        /// <summary>
        /// Removes the element with the specified key from the hash table.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>True if the element was removed; otherwise, false.</returns>
        public bool Remove(TKey key)
        {
            int bucketIndex = _hashFunction.ComputeHash(key, _numberOfBuckets);
            var bucket = _buckets[bucketIndex];

            var node = bucket.First;
            while (node != null)
            {
                if (node.Value.Key.Equals(key))
                {
                    bucket.Remove(node);
                    return true;
                }
                node = node.Next;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the hash table contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns>True if the hash table contains an element with the specified key; otherwise, false.</returns>
        public bool ContainsKey(TKey key)
        {
            int bucketIndex = _hashFunction.ComputeHash(key, _numberOfBuckets);
            var bucket = _buckets[bucketIndex];

            foreach (var node in bucket)
            {
                if (node.Key.Equals(key))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the number of key-value pairs contained in the hash table.
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;
                foreach (var bucket in _buckets)
                {
                    count += bucket.Count;
                }
                return count;
            }
        }

        /// <summary>
        /// Gets an enumerable collection of keys in the hash table.
        /// </summary>
        public IEnumerable<TKey> Keys
        {
            get
            {
                foreach (var bucket in _buckets)
                {
                    foreach (var node in bucket)
                    {
                        yield return node.Key;
                    }
                }
            }
        }

        /// <summary>
        /// Gets an enumerable collection of values in the hash table.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                foreach (var bucket in _buckets)
                {
                    foreach (var node in bucket)
                    {
                        yield return node.Value;
                    }
                }
            }
        }
    }
}
