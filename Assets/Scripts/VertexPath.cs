using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[ExecuteInEditMode]
public class VertexPath : MonoBehaviour
{
    [Header("Edge placement")]
    public float minEdgeAngle = 90f;

    [HideInInspector]
    public List<Edge> edges = new List<Edge>();

    [HideInInspector]
    public List<Vector3> vertices = new List<Vector3>();

    private VertexNetwork net;
    private float travelerScale = 1f;
    private float distance;
    private bool running;
    private bool runningLeft;
    private GameObject train;

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
        net = vertexNetwork;
        this.travelerScale = travelerScale;
        this.minEdgeAngle = minEdgeAngle;
        edge = edge.DirectionalFrom(root);
        vertices.Add(edge.fromVertex);
        vertices.Add(edge.toVertex);
        AddEdge(edge);
    }

    private void AddEdge(Edge edge)
    {
        edges.Add(edge);
        var edgeModel = Instantiate(net.edgeModelPrefab, transform);
        edgeModel.transform.localScale = new Vector3(
            travelerScale,
            travelerScale / 2f,
            edge.length
        );
        edgeModel.transform.position = edge.middle;
        edgeModel.transform.rotation = Quaternion.LookRotation(edge.extent, Vector3.up);
    }

    public Edge LastEdge()
    {
        return edges[edges.Count - 1];
    }

    public Vector3 LastVertex()
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

        var connectableEdges = net.ConnectableEdges(this);
        bool hasConnectable = false;

        foreach (var connEdge in connectableEdges)
        {
            if (connEdge.Equals(edge))
            {
                hasConnectable = true;
            }
        }

        if (!hasConnectable)
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
        if (distance > TotalTrackLength() - edge.length)
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
        AddEdge(directionalEdge);
        return true;
    }

    public void Join(VertexPath path)
    {
        path.vertices.Reverse();
        path.edges.Reverse();
        vertices.AddRange(path.vertices);
        foreach (var edge in path.edges)
        {
            AddEdge(edge.DirectionalFrom(LastEdge().toVertex));
        }
    }

    private float TotalTrackLength()
    {
        float len = 0;
        foreach (var edge in edges)
        {
            len += edge.length;
        }
        return len;
    }

    public bool IsComplete()
    {
        return IsValidPath()
            && net.rootVectors.Contains(vertices[0])
            && net.rootVectors.Contains(LastVertex());
    }

    private bool IsValidPath()
    {
        return vertices != null & vertices.Count >= 2;
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

    private void SpawnTrain()
    {
        train = GameObject.Instantiate(net.trainPrefab, transform);
        train.transform.localScale *= travelerScale;
        train.transform.position = vertices[0];
    }

    public void StartMoving()
    {
        SpawnTrain();
        train.transform.position = vertices[0];
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

            float totalTrackLength = TotalTrackLength();

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
        Edge foundEdge = null;
        Vector3 movePos = Vector3.zero;

        Assert.AreNotEqual(vertices.Count, 0);
        foreach (var edge in edges)
        {
            if (edgeDistance <= edge.length)
            {
                movePos = Vector3.Lerp(edge.fromVertex, edge.toVertex, edgeDistance / edge.length);
                foundEdge = edge;
                break;
            }
            edgeDistance -= edge.length;
        }

        if (foundEdge == null)
        {
            movePos = vertices[vertices.Count - 1];
        }

        train.transform.position = movePos;
        train.transform.rotation = Quaternion.LookRotation(foundEdge.extent, Vector3.up);
    }

    private void CompletedTrip()
    {
        if (IsComplete())
        {
            SingletonProvider.Get<EconomyController>().GiveResources(1);
        }
    }
}
