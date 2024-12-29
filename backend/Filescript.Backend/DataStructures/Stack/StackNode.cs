namespace Filescript.Backend.DataStructures.Stack
{
    /// <summary>
    /// Represents a node in a stack.
    /// </summary>
    /// <typeparam name="T">Type of the value stored in the node.</typeparam>
    public class StackNode<T>
    {
        /// <summary>
        /// Gets or sets the value contained in the node.
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// Gets or sets the next node in the stack.
        /// </summary>
        public StackNode<T> Next { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StackNode{T}"/> class with a specified value.
        /// </summary>
        /// <param name="value">The value to store in the node.</param>
        public StackNode(T value)
        {
            Value = value;
            Next = null;
        }
    }
}
