using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HashSet-style data structure that tracks refcounts of items. Items are only removed
/// when their refcounts reach 0.
/// </summary>
/// <typeparam name="T"></typeparam>
public class RefCountSet<T>
{
    private Dictionary<T, int> counts = new Dictionary<T, int>();

    /// <summary>
    /// Increases the refcount of an item by 1, adding it to the set if it isn't already there.
    /// Returns true if this is the first time the item has been added to the set.
    /// </summary>
    public bool Add(T item)
    {
        bool isNew = !counts.ContainsKey(item);
        if (isNew)
        {
            counts[item] = 1;
        }
        else
        {
            counts[item]++;
        }
        return isNew;
    }

    /// <summary>
    /// Decreases the refcount of an item by 1, removing it from the set if the count reaches 0.
    /// Returns true if the item was deleted.
    /// </summary>
    public bool Remove(T item)
    {
        if (!counts.ContainsKey(item))
        {
            Debug.LogWarningFormat("RefCountSet does not have item {0} to remove", item);
            return false;
        }

        if (--counts[item] == 0)
        {
            counts.Remove(item);
            return true;
        }

        return false;
    }

    public bool Contains(T item)
    {
        return counts.ContainsKey(item);
    }
}
