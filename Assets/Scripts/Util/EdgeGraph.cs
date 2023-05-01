using System.Collections.Generic;
using UnityEngine;

public class EdgeGraph
{
    public List<Vector3> vertices;
    public List<Edge> edges;

    public EdgeGraph(List<Edge> ed)
    {
        edges = new List<Edge>(ed);
        edges.Sort();
        Lists.Uniq(edges);
        RebuildVertices();
    }

    private void RebuildVertices() {
        vertices = new List<Vector3>();
        foreach (var edge in edges) {
            vertices.Add(edge.left);
            vertices.Add(edge.right);
        }
        vertices.Sort(Vectors.Compare);
        Lists.Uniq(vertices);
    }

    public Edge ClosestEdge(Vector3 point)
    {
        Edge closest = null;
        float min = float.PositiveInfinity;
        foreach (var edge in edges)
        {
            var distance = edge.Distance(point);
            if (distance < min)
            {
                closest = edge;
                min = distance;
            }
        }
        return closest;
    }

    public List<Edge> AdjacentEdges(Vector3 point)
    {
        var adj = new List<Edge>();
        foreach (var edge in edges)
        {
            if (point == edge.left || point == edge.right)
            {
                adj.Add(edge);
            }
        }
        return adj;
    }

    public Vector3 ClosestVertex(Vector3 point)
    {
        Vector3 closest = Vector3.zero;
        float min = float.PositiveInfinity;
        foreach (var vertex in vertices)
        {
            var distance = Vector3.Distance(vertex, point);
            if (distance < min)
            {
                closest = vertex;
                min = distance;
            }
        }
        return closest;
    }

    public Edge FindEdge(Vector3 l, Vector3 r)
    {
        var findEdge = new Edge(l, r);
        foreach (var edge in edges)
        {
            if (edge.Equals(findEdge))
            {
                return edge;
            }
        }
        Debug.LogError("couldn't find an edge!!!!!! this is going to break in unexpected ways");
        return findEdge;
    }

    public void RemoveMidpointEdges(Vector3 mid)
    {
        // Adjacent edges are ones where both vertices are close to the point - that is,
        // roughly, less than 1 edge's distance from them.
        var newEdges = new List<Edge>();

        foreach (var edge in edges)
        {
            if (
                Vector3.Distance(edge.left, mid) > edge.length
                || Vector3.Distance(edge.right, mid) > edge.length
            )
            {
                newEdges.Add(edge);
            }
        }

        edges = newEdges;
        RebuildVertices();
    }
}
