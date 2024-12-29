using System;

namespace Filescript.Backend.DataStructures.Stack
{
    /// <summary>
    /// Represents a generic stack data structure.
    /// </summary>
    /// <typeparam name="T">Type of elements stored in the stack.</typeparam>
    public class Stack<T>
    {
        /// <summary>
        /// Gets the top node of the stack.
        /// </summary>
        public StackNode<T> Top { get; private set; }

        /// <summary>
        /// Gets the number of elements in the stack.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Stack{T}"/> class.
        /// </summary>
        public Stack()
        {
            Top = null;
            Count = 0;
        }

        /// <summary>
        /// Pushes a new element onto the top of the stack.
        /// </summary>
        /// <param name="value">The value to push.</param>
        public void Push(T value)
        {
            var newNode = new StackNode<T>(value);
            newNode.Next = Top;
            Top = newNode;
            Count++;
        }

        /// <summary>
        /// Pops the top element from the stack.
        /// </summary>
        /// <returns>The value of the popped element.</returns>
        /// <exception cref="InvalidOperationException">Thrown when attempting to pop from an empty stack.</exception>
        public T Pop()
        {
            if (Top == null)
                throw new InvalidOperationException("Cannot pop from an empty stack.");

            T value = Top.Value;
            Top = Top.Next;
            Count--;
            return value;
        }

        /// <summary>
        /// Peeks at the top element without removing it.
        /// </summary>
        /// <returns>The value of the top element.</returns>
        /// <exception cref="InvalidOperationException">Thrown when attempting to peek into an empty stack.</exception>
        public T Peek()
        {
            if (Top == null)
                throw new InvalidOperationException("Cannot peek into an empty stack.");

            return Top.Value;
        }

        /// <summary>
        /// Determines whether the stack is empty.
        /// </summary>
        /// <returns>True if the stack is empty; otherwise, false.</returns>
        public bool IsEmpty()
        {
            return Top == null;
        }

        /// <summary>
        /// Clears all elements from the stack.
        /// </summary>
        public void Clear()
        {
            Top = null;
            Count = 0;
        }
    }
}
