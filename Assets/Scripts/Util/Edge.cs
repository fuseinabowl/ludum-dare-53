using System;
using UnityEngine;

/// <summary>
/// An edge is represented by 2 vectors. Edges internally guarantee that left < right.
/// </summary>
public class Edge : IEquatable<Edge>, IComparable<Edge>
{
    public Vector3 left { get; private set; }
    public Vector3 right { get; private set; }
    public Vector3 extent { get; private set; }
    public float length { get; private set; }

    public Edge(Vector3 l, Vector3 r)
    {
        if (Vectors.Compare(l, r) > 0)
        {
            left = r;
            right = l;
        }
        else
        {
            left = l;
            right = r;
        }
        extent = right - left;
        length = Vector3.Distance(left, right);
    }

    /// <summary>
    /// Calculates the distance between this edge and a point via the point's normal to this edge.
    /// </summary>
    public float Distance(Vector3 point) {
        var projection = Vectors.ProjectPointLine(point, left, right);
        return Vector3.Distance(point, projection);
    }

    public override bool Equals(object obj)
    {
        return obj is Edge edge && left.Equals(edge.left) && right.Equals(edge.right);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(left, right);
    }

    public bool Equals(Edge other)
    {
        return left.Equals(other.left) && right.Equals(other.right);
    }

    public int CompareTo(Edge other)
    {
        if (left != other.left)
        {
            return Vectors.Compare(left, other.left);
        }
        return Vectors.Compare(right, other.right);
    }
}