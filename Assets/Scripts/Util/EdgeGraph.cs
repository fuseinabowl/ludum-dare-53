using System.Collections.Generic;
using UnityEngine;

public class EdgeGraph {
    public List<Vector3> vertices;
    public List<Edge> edges;

    public EdgeGraph(List<Vector3> vert, List<Edge> ed) {
        vertices = new List<Vector3>(vert);
        vertices.Sort(Vectors.Compare);
        Lists.Uniq(vertices);
        edges = new List<Edge>(ed);
        edges.Sort();
        Lists.Uniq(edges);
    }

    public Edge ClosestEdge(Vector3 point) {
        Edge closest = null;
        float min = float.PositiveInfinity;
        foreach (var edge in edges) {
            var distance = edge.Distance(point);
            if (distance < min) {
                closest = edge;
                min = distance;
            }
        }
        return closest;
    }
}