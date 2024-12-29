using System;

namespace Filescript.Backend.DataStructures.LinkedList
{
    /// <summary>
    /// Represents a singly linked list.
    /// </summary>
    /// <typeparam name="T">Type of elements stored in the linked list.</typeparam>
    public class LinkedList<T>
    {
        /// <summary>
        /// Gets the head node of the linked list.
        /// </summary>
        public LinkedListNode<T>? Head { get; private set; }

        /// <summary>
        /// Gets the tail node of the linked list.
        /// </summary>
        public LinkedListNode<T>? Tail { get; private set; }

        /// <summary>
        /// Gets the number of elements in the linked list.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinkedList{T}"/> class.
        /// </summary>
        public LinkedList()
        {
            Head = null;
            Tail = null;
            Count = 0;
        }

        /// <summary>
        /// Adds a new node with the specified value at the end of the linked list.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void AddLast(T value)
        {
            var newNode = new LinkedListNode<T>(value);
            if (Head == null)
            {
                Head = newNode;
                Tail = Head;
            }
            else
            {
                Tail.Next = newNode;
                Tail = newNode;
            }
            Count++;
        }

        /// <summary>
        /// Adds a new node with the specified value at the beginning of the linked list.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void AddFirst(T value)
        {
            var newNode = new LinkedListNode<T>(value);
            if (Head == null)
            {
                Head = newNode;
                Tail = Head;
            }
            else
            {
                newNode.Next = Head;
                Head = newNode;
            }
            Count++;
        }

        /// <summary>
        /// Removes the first occurrence of a node with the specified value.
        /// </summary>
        /// <param name="value">The value to remove.</param>
        /// <returns>True if a node was removed; otherwise, false.</returns>
        public bool Remove(T value)
        {
            if (Head == null)
                return false;

            if (Head.Value.Equals(value))
            {
                Head = Head.Next;
                if (Head == null)
                    Tail = null;
                Count--;
                return true;
            }

            var current = Head;
            while (current.Next != null && !current.Next.Value.Equals(value))
            {
                current = current.Next;
            }

            if (current.Next != null)
            {
                current.Next = current.Next.Next;
                if (current.Next == null)
                    Tail = current;
                Count--;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the first node containing the specified value.
        /// </summary>
        /// <param name="value">The value to find.</param>
        /// <returns>The node containing the value, or null if not found.</returns>
        public LinkedListNode<T>? Find(T value)
        {
            var current = Head;
            while (current != null)
            {
                if (current.Value.Equals(value))
                    return current;
                current = current.Next;
            }
            return null;
        }

        /// <summary>
        /// Clears all nodes from the linked list.
        /// </summary>
        public void Clear()
        {
            Head = null;
            Tail = null;
            Count = 0;
        }
    }
}
