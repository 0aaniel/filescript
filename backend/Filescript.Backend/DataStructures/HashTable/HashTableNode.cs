namespace Filescript.Backend.DataStructures.HashTable
{
    /// <summary>
    /// Represents a node in the hash table's bucket.
    /// </summary>
    /// <typeparam name="TKey">Type of the key.</typeparam>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    public class HashTableNode<TKey, TValue>
    {
        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        public TKey Key { get; set; }

        /// <summary>
        /// Gets or sets the value associated with the key.
        /// </summary>
        public TValue Value { get; set; }

        /// <summary>
        /// Gets or sets the next node in the bucket.
        /// </summary>
        public HashTableNode<TKey, TValue>? Next { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HashTableNode{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public HashTableNode(TKey key, TValue value)
        {
            Key = key;
            Value = value;
            Next = null;
        }
    }
}
