using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class VertexPath : MonoBehaviour
{
    [Header("Nodes")]
    public GameObject traveler;

    [Header("Edge placement")]
    public float minEdgeAngle = 90f;

    [HideInInspector]
    public List<Edge> edges = new List<Edge>();

    [HideInInspector]
    public List<Vector3> vertices = new List<Vector3>();

    private VertexNetwork net;
    private float travelerScale = 1f;
    private float totalTrackLength = 0;
    private float distance;
    private bool running;
    private bool runningLeft;

    public void Init(
        VertexNetwork vertexNetwork,
        Vector3 root,
        Edge edge,
        float travelerScale,
        float minEdgeAngle
    )
    {
        Debug.Assert(vertices.Count == 0);
        Debug.Assert(edges.Count == 0);
        edge = edge.DirectionalFrom(root);
        vertices.Add(edge.fromVertex);
        vertices.Add(edge.toVertex);
        edges.Add(edge);
        totalTrackLength = edge.length;
        net = vertexNetwork;
        this.travelerScale = travelerScale;
        this.minEdgeAngle = minEdgeAngle;
        traveler.transform.localScale = travelerScale * Vector3.one;
        traveler.transform.position = vertices[0];
    }

    private Edge LastEdge()
    {
        return edges[edges.Count - 1];
    }

    private Vector3 LastVertex()
    {
        return vertices[vertices.Count - 1];
    }

    public bool CanConnect(Edge edge)
    {
        if (!IsValidPath())
        {
            Debug.Log("can't connect, isn't a valid path");
            return false;
        }

        var lastVertex = LastVertex();
        if (edge.left != lastVertex && edge.right != lastVertex)
        {
            return false;
        }

        Debug.Assert(edge.direction == Edge.Direction.NONE);
        var lastEdge = LastEdge();
        var directionalEdge = edge.DirectionalFrom(lastVertex);

        if (Vector3.Angle(lastEdge.extent, -directionalEdge.extent) < minEdgeAngle)
        {
            return false;
        }

        return true;
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
        var lastVertex = LastVertex();
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
        var lastEdge = LastEdge();
        return lastEdge.NonDirectional().Equals(edge);
    }

    public bool EndsWith(Vector3 vertex)
    {
        return LastVertex() == vertex;
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
        var directionalEdge = edge.DirectionalFrom(LastVertex());
        vertices.Add(directionalEdge.toVertex);
        edges.Add(directionalEdge);
        totalTrackLength += directionalEdge.length;
        return true;
    }

    public bool IsComplete()
    {
        return IsValidPath()
            && net.rootVectors.Contains(vertices[0])
            && net.rootVectors.Contains(LastVertex());
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
                distance -= net.moveSpeed * Time.deltaTime;
            }
            else
            {
                distance += net.moveSpeed * Time.deltaTime;
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
                CompletedTrip();
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

    private void CompletedTrip()
    {
        if (IsComplete())
        {
            SingletonProvider.Get<EconomyController>().GiveResources(1);
        }
    }
}
