using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An edge, optionally directional, represented by 2 vectors plus a direction if directional.
/// </summary>
public class Edge : IEquatable<Edge>, IComparable<Edge>
{
    /// <summary>
    /// Support for directional graphs. For non-directional graphs, which is the default, use NONE.
    /// </summary>
    public enum Direction
    {
        NONE, // non-directional
        LTR, // left -> right directional
        RTL // right -> left directional
    }

    /// <summary>
    /// Left vertex of the edge.
    /// </summary>
    public Vector3 left { get; private set; }

    /// <summary>
    /// Right vertext of the edge.
    /// </summary>
    public Vector3 right { get; private set; }

    /// <summary>
    /// The vertex that the extent starts from. This will be either left or right depending on
    /// the edge's direction. NONE direction defaults to LTR.
    /// </summary>
    public Vector3 fromVertex
    {
        get
        {
            if (direction == Direction.RTL)
            {
                return right;
            }
            return left;
        }
        private set { }
    }

    /// <summary>
    /// The vertex that the extent goes to. This will be either left or right depending on
    /// the edge's direction. NONE direction defaults to LTR.
    /// </summary>
    public Vector3 toVertex
    {
        get
        {
            if (direction == Direction.RTL)
            {
                return left;
            }
            return right;
        }
        private set { }
    }

    /// <summary>
    /// The lesser of left and right via Vector comparison. Useful for non-directional comparison.
    /// </summary>
    /// <value></value>
    public Vector3 minVertex
    {
        get
        {
            if (Vectors.Compare(left, right) < 0)
            {
                return left;
            }
            return right;
        }
        private set { }
    }

    /// <summary>
    /// The greater of left and right via Vector comparison. Useful for non-directional comparison.
    /// </summary>
    /// <value></value>
    public Vector3 maxVertex
    {
        get
        {
            if (Vectors.Compare(left, right) > 0)
            {
                return left;
            }
            return right;
        }
        private set { }
    }

    /// <summary>
    /// Direction of the edge. NONE means a non-directional graph, and is the default.
    /// </summary>
    public Direction direction { get; private set; }

    /// <summary>
    /// Extent of the graph. For non-directional graphs this will be left -> right by convention,
    /// and for directional graphs this will depend on the direction.
    /// </summary>
    public Vector3 extent
    {
        get
        {
            if (direction == Direction.RTL)
            {
                return left - right;
            }
            return right - left;
        }
        private set { }
    }

    /// <summary>
    /// The length of the edge from left -> right.
    /// </summary>
    public float length { get; private set; }

    public Edge(Vector3 l, Vector3 r, Direction dir = Direction.NONE)
    {
        left = l;
        right = r;
        direction = dir;
        length = Vector3.Distance(left, right);
    }

    /// <summary>
    /// Calculates the distance between this edge and a point via the point's normal to this edge.
    /// </summary>
    public float Distance(Vector3 point)
    {
        var projection = Vectors.ProjectPointLine(point, left, right);
        return Vector3.Distance(point, projection);
    }

    public Vector3 OtherVertex(Vector3 vertex)
    {
        return vertex == left ? right : left;
    }

    /// <summary>
    /// Returns a new Edge with the same vertices but direction modified so that the fromVertex is
    /// equal to vector.
    /// </summary>
    public Edge DirectionalFrom(Vector3 vertex)
    {
        return new Edge(left, right, vertex == left ? Direction.LTR : Direction.RTL);
    }

    /// <summary>
    /// Converts this Edge to a non-directional edge, conforming to the convention that NONE
    /// implies an LTR direction.
    /// </summary>
    /// <returns></returns>
    public Edge NonDirectional() {
        if (direction == Direction.RTL) {
            return new Edge(right, left, Direction.NONE);
        }
        return new Edge(left, right, Direction.NONE);
    }

    public override bool Equals(object obj)
    {
        return obj is Edge edge && Equals(edge);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(left, right, direction);
    }

    public bool Equals(Edge other)
    {
        return CompareTo(other) == 0;
    }

    public int CompareTo(Edge other)
    {
        var directionCompare = direction.CompareTo(other.direction);
        if (directionCompare != 0)
        {
            return directionCompare;
        }
        if (direction == Direction.NONE)
        {
            return CompareVertexPairs(minVertex, maxVertex, other.minVertex, other.maxVertex);
        }
        return CompareVertexPairs(left, right, other.left, other.right);
    }

    private static int CompareVertexPairs(Vector3 a, Vector3 b, Vector3 otherA, Vector3 otherB)
    {
        var aCompare = Vectors.Compare(a, otherA);
        if (aCompare != 0)
        {
            return aCompare;
        }
        return Vectors.Compare(b, otherB);
    }
}
