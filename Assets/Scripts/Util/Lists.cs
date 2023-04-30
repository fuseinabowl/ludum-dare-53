using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// `List` utilities.
/// </summary>
public class Lists
{
    /// <summary>
    /// Returns a List made up of `items`.
    ///
    /// For example: List.Of(1, 2, 3), List.Of("item").
    /// </summary>
    public static List<T> Of<T>(params T[] items)
    {
        return new List<T>(items);
    }

    /// <summary>
    /// Removes *adjacent* duplicate entries a list. This makes the most sense when called on a
    /// sorted list.
    /// </summary>
    public static void Uniq<T>(List<T> list)
        where T : IEquatable<T>
    {
        for (int i = 1; i < list.Count; )
        {
            if (list[i - 1].Equals(list[i]))
            {
                list.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }
    }

    /// <summary>
    /// Gets the list item at an index while treating it as a circular list.
    /// /// This means that accesses that are usually out of bounds (negative, greater than length)
    /// actually resolve to their circular value.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="i"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T Circ<T>(List<T> list, int i)
    {
        while (i < 0)
        {
            i += list.Count;
        }
        return list[i % list.Count];
    }
}
