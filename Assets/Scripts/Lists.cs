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
    /// <example>
    /// List.Of(1, 2, 3);
    /// List.Of("item");
    /// </example>
    /// </summary>
    public static List<T> Of<T>(params T[] items)
    {
        return new List<T>(items);
    }
}
