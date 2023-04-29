using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class VertexPath : MonoBehaviour
{
    [Header("Nodes")]
    public GameObject traveler;

    [Header("Movement")]
    public float moveSpeed = 1f;

    [HideInInspector]
    public List<Edge> edges = new List<Edge>();

    [HideInInspector]
    public List<Vector3> vertices = new List<Vector3>();

    private float travelerScale = 1f;
    private float totalTrackLength = 0;
    private float distance;
    private bool running;
    private bool runningLeft;

    // public void AddLeftVertex(Vector3 vertex) {
    //     vertices.Insert(0, vertex);
    //     CreateEdges();
    //     distance += edges[0].length;
    // }

    // public void AddRightVertex(Vector3 vertex) {
    //     vertices.Add(vertex);
    //     CreateEdges();
    // }

    // public void RemoveLeftVertex() {
    //     if (!IsValidPath()) {
    //         return;
    //     }
    //     distance -= edges[0].length;
    //     if (distance < 0) {
    //         // the game should probably prevent this from happening (the track that the train is on was deleted)
    //         distance = 0;
    //     }
    //     vertices.RemoveAt(0);
    //     CreateEdges();
    // }

    // public void RemoveRightVertex() {
    //     if (!IsValidPath()) {
    //         return;
    //     }
    //     vertices.RemoveAt(vertices.Count - 1);
    //     CreateEdges();
    //     if (distance > totalTrackLength) {
    //         // the game should probably prevent this from happening (the track that the train is on was deleted)
    //         distance = totalTrackLength;
    //     }
    // }

    public void Init(Vector3 connectPoint, Edge edge, float travelerScale = 1f)
    {
        Debug.Assert(vertices.Count == 0);
        Debug.Assert(edges.Count == 0);
        vertices.Add(connectPoint);
        vertices.Add(edge.Alternate(connectPoint));
        edges.Add(edge);
        totalTrackLength = edge.length;
        this.travelerScale = travelerScale;
        traveler.transform.localScale = travelerScale * Vector3.one;
        traveler.transform.position = vertices[0];
    }

    public bool CanConnect(Edge edge)
    {
        if (!IsValidPath())
        {
            return false;
        }
        var lastVertex = vertices[vertices.Count - 1];
        // return edge.left == vertices[0] || edge.left == lastVertex || edge.right == vertices[0] || edge.right == lastVertex;
        return edge.left == lastVertex || edge.right == lastVertex;
    }

    public bool ContainsEdge(Edge edge)
    {
        foreach (var pathEdge in edges)
        {
            if (edge.Equals(pathEdge))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// True if this path has a vertex that would overlap with edge other than at the ends.
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    public bool HasInternalVertexOnEdge(Edge edge) {
        for (int i = 1; i < vertices.Count - 1; i++) {
            if (vertices[i] == edge.left || vertices[i] == edge.right) {
                return true;
            }
        }
        return false;
    }

    public bool CompletesLoop(Edge edge) {
        var lastVertex = vertices[vertices.Count - 1];
        if (edge.left == vertices[0]) {
            return edge.right == lastVertex;
        }
        if (edge.right == vertices[0]) {
            return edge.left == lastVertex;
        }
        return false;
    }

    public bool Connect(Edge edge)
    {
        if (!CanConnect(edge))
        {
            return false;
        }
        // if (edge.left == vertices[0] || edge.right == vertices[0]) {
        //     vertices.Insert(0, edge.Alternate(vertices[0]));
        //     edges.Insert(0, edge);
        //     totalTrackLength += edge.length;
        //     distance += edge.length;
        // } else {
        vertices.Add(edge.Alternate(vertices[vertices.Count - 1]));
        edges.Add(edge);
        totalTrackLength += edge.length;
        // }
        return true;
    }

    private bool IsValidPath()
    {
        return traveler != null && vertices != null & vertices.Count >= 2;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (var vertex in vertices)
        {
            Gizmos.DrawSphere(vertex, travelerScale);
        }
        Gizmos.color = Color.blue;
        foreach (var edge in edges)
        {
            Gizmos.DrawLine(edge.left, edge.right);
        }
    }

    public void StartMoving()
    {
        traveler.transform.position = vertices[0];
        distance = 0;
        running = true;
        runningLeft = false;
        StartCoroutine(MoveCoroutine());
    }

    private IEnumerator MoveCoroutine()
    {
        while (running)
        {
            if (!IsValidPath())
            {
                yield return null;
            }

            if (runningLeft)
            {
                distance -= moveSpeed * Time.deltaTime;
            }
            else
            {
                distance += moveSpeed * Time.deltaTime;
            }

            if (runningLeft && distance < 0)
            {
                runningLeft = false;
                distance = 0;
            }
            else if (!runningLeft && distance > totalTrackLength)
            {
                runningLeft = true;
                distance = totalTrackLength;
            }

            MoveTraveler(distance);
            yield return null;
        }
    }

    private void MoveTraveler(float distance)
    {
        float edgeDistance = distance;
        bool found = false;
        Vector3 movePos = Vector3.zero;

        for (int i = 0; i < edges.Count; i++)
        {
            var vertex = vertices[i];
            var edge = edges[i];
            if (edgeDistance <= edge.length)
            {
                movePos = Vector3.Lerp(vertex, edge.Alternate(vertex), edgeDistance / edge.length);
                found = true;
                break;
            }
            edgeDistance -= edge.length;
        }

        if (!found)
        {
            Debug.LogWarningFormat("Didn't find position at {0}", distance);
            movePos = vertices[0];
        }

        traveler.transform.position = movePos;
    }
}
