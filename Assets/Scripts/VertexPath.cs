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

    [Header("Edge placement")]
    public float minEdgeAngle = 90f;

    [HideInInspector]
    public List<Edge> edges = new List<Edge>();

    [HideInInspector]
    public List<Vector3> vertices = new List<Vector3>();

    private float travelerScale = 1f;
    private float totalTrackLength = 0;
    private float distance;
    private bool running;
    private bool runningLeft;

    public void Init(Vector3 connectPoint, Edge edge, float travelerScale, float minEdgeAngle)
    {
        Debug.Assert(vertices.Count == 0);
        Debug.Assert(edges.Count == 0);
        vertices.Add(connectPoint);
        vertices.Add(edge.OtherVertex(connectPoint));
        edges.Add(edge);
        totalTrackLength = edge.length;
        this.travelerScale = travelerScale;
        this.minEdgeAngle = minEdgeAngle;
        traveler.transform.localScale = travelerScale * Vector3.one;
        traveler.transform.position = vertices[0];
    }

    public bool CanConnect(Edge edge)
    {
        if (!IsValidPath())
        {
            Debug.Log("can't connect, isn't a valid path");
            return false;
        }

        var lastVertex = vertices[vertices.Count - 1];
        if (edge.left != lastVertex && edge.right != lastVertex)
        {
            return false;
        }

        Debug.Assert(edge.direction == Edge.Direction.NONE);
        var lastEdge = edges[edges.Count - 1];
        var directionalEdge = edge.DirectionalFrom(lastVertex);

        // This comparison is "> minEdgeAngle" rather than "< maxEdgeAngle" because it compares the
        // extent of each vector's to->from vectors. Imagine a straight track composing those on top
        // of each other. The angle is 0! Now imagine a track that makes a sharp 45-degree turn and
        // composing THOSE on top of each other. The angle is 135!
        if (Vector3.Angle(lastEdge.extent, directionalEdge.extent) > minEdgeAngle)
        {
            return false;
        }

        return true;
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
    public bool HasInternalVertexOnEdge(Edge edge)
    {
        for (int i = 1; i < vertices.Count - 1; i++)
        {
            if (vertices[i] == edge.left || vertices[i] == edge.right)
            {
                return true;
            }
        }
        return false;
    }

    public bool CompletesLoop(Edge edge)
    {
        var lastVertex = vertices[vertices.Count - 1];
        if (edge.left == vertices[0])
        {
            return edge.right == lastVertex;
        }
        if (edge.right == vertices[0])
        {
            return edge.left == lastVertex;
        }
        return false;
    }

    public bool CanDeleteEdge(Edge edge)
    {
        if (edges.Count == 1)
        {
            // can't delete the last edge
            return false;
        }
        if (distance > totalTrackLength - edge.length)
        {
            // can't delete edge if the train is on it
            return false;
        }
        var lastEdge = edges[edges.Count - 1];
        return lastEdge.NonDirectional().Equals(edge);
    }

    public bool EndsWith(Vector3 vertex)
    {
        return vertices[vertices.Count - 1] == vertex;
    }

    public bool DeleteEdge(Edge edge)
    {
        if (!CanDeleteEdge(edge))
        {
            return false;
        }
        edges.RemoveAt(edges.Count - 1);
        vertices.RemoveAt(vertices.Count - 1);
        totalTrackLength -= edge.length;
        return true;
    }

    /// <summary>
    /// Note that this will modify the edge's direction.
    /// </summary>
    public bool Connect(Edge edge)
    {
        if (!CanConnect(edge))
        {
            return false;
        }
        Debug.Assert(edge.direction == Edge.Direction.NONE);
        var lastVertex = vertices[vertices.Count - 1];
        var directionalEdge = edge.DirectionalFrom(lastVertex);
        vertices.Add(directionalEdge.toVertex);
        edges.Add(directionalEdge);
        totalTrackLength += directionalEdge.length;
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

        // for (int i = 0; i < edges.Count; i++)
        // {
        //     var vertex = vertices[i];
        //     var edge = edges[i];
        //     if (edgeDistance <= edge.length)
        //     {
        //         movePos = Vector3.Lerp(
        //             vertex,
        //             edge.OtherVertex(vertex),
        //             edgeDistance / edge.length
        //         );
        //         found = true;
        //         break;
        //     }
        //     edgeDistance -= edge.length;
        // }

        foreach (var edge in edges)
        {
            if (edgeDistance <= edge.length)
            {
                movePos = Vector3.Lerp(edge.fromVertex, edge.toVertex, edgeDistance / edge.length);
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
