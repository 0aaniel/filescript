using System;

namespace Filescript.Backend.DataStructures.HashTable
{
    /// <summary>
    /// Represents a simple hash function.
    /// </summary>
    public class HashFunction<TKey>
    {
        /// <summary>
        /// Computes the hash code for the specified key and maps it to a bucket index.
        /// </summary>
        /// <param name="key">The key to hash.</param>
        /// <param name="numberOfBuckets">The total number of buckets in the hash table.</param>
        /// <returns>The bucket index.</returns>
        public int ComputeHash(TKey key, int numberOfBuckets)
        {
            if (numberOfBuckets <= 0)
                throw new ArgumentException("Number of buckets must be positive.", nameof(numberOfBuckets));

            int hashCode = key.GetHashCode();
            // Ensure the hash code is non-negative
            hashCode &= 0x7FFFFFFF;
            return hashCode % numberOfBuckets;
        }
    }
}
