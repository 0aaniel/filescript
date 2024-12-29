namespace Filescript.Backend.DataStructures.LinkedList
{
    /// <summary>
    /// Represents a node in a linked list.
    /// </summary>
    /// <typeparam name="T">Type of the value stored in the node.</typeparam>
    public class LinkedListNode<T>
    {
        /// <summary>
        /// Gets or sets the value contained in the node.
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// Gets or sets the next node in the linked list.
        /// </summary>
        public LinkedListNode<T>? Next { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinkedListNode{T}"/> class with a specified value.
        /// </summary>
        /// <param name="value">The value to store in the node.</param>
        public LinkedListNode(T value)
        {
            Value = value;
            Next = null;
        }
    }
}
