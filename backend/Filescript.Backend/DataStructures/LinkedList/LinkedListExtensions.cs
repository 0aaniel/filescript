using System;
using System.Collections.Generic;
using Filescript.Backend.DataStructures.LinkedList;

namespace Filescript.Backend.DataStructures.LinkedList
{
    /// <summary>
    /// Provides extension methods for the LinkedList class.
    /// </summary>
    public static class LinkedListExtensions
    {
        /// <summary>
        /// Converts the linked list to an array.
        /// </summary>
        /// <typeparam name="T">Type of elements in the linked list.</typeparam>
        /// <param name="list">The linked list to convert.</param>
        /// <returns>An array containing the elements of the linked list.</returns>
        public static T[] ToArray<T>(this LinkedList<T> list)
        {
            T[] array = new T[list.Count];
            int index = 0;
            var current = list.Head;
            while (current != null)
            {
                array[index++] = current.Value;
                current = current.Next;
            }
            return array;
        }

        /// <summary>
        /// Iterates through the linked list and performs an action on each element.
        /// </summary>
        /// <typeparam name="T">Type of elements in the linked list.</typeparam>
        /// <param name="list">The linked list to iterate over.</param>
        /// <param name="action">The action to perform on each element.</param>
        public static void ForEach<T>(this LinkedList<T> list, Action<T> action)
        {
            var current = list.Head;
            while (current != null)
            {
                action(current.Value);
                current = current.Next;
            }
        }
    }
}
