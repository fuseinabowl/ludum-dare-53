using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[ExecuteInEditMode]
public class VertexPath : MonoBehaviour
{
    public class EdgeAndInstanceData
    {
        public Edge edge;
        public GameObject tracksObject;
    }

    [HideInInspector]
    public List<EdgeAndInstanceData> edges = new List<EdgeAndInstanceData>();

    [HideInInspector]
    public List<Vector3> vertices = new List<Vector3>();

    private VertexNetwork net;
    private GameObject train;
    private float trainDistance;
    private bool trainRunning;
    private bool trainRunningLeft;

    public void Init(VertexNetwork vertexNetwork, Vector3 root, Edge edge)
    {
        Debug.Assert(vertices.Count == 0);
        Debug.Assert(edges.Count == 0);
        net = vertexNetwork;
        edge = edge.DirectionalFrom(root);
        AddEdge(edge);
        BuildVertices();
    }

    private void BuildVertices()
    {
        vertices.Clear();
        vertices.Add(edges[0].edge.fromVertex);
        foreach (var edge in edges)
        {
            vertices.Add(edge.edge.toVertex);
        }
    }

    private void AddEdge(Edge edge)
    {
        var edgeModel = Instantiate(net.edgeModelPrefab, transform);
        edgeModel.GetComponentInChildren<TrackModelGeneratorComponent>().SetPoints(
            edge.middle,
            edge.toVertex,
            CalculateOverExtendedVertex(edge.middle, edge.toVertex, overextendDistance:0.1f)
        );

        UpdateLastEdgeEndPoint(edge.middle);
        edges.Add(new EdgeAndInstanceData{
            edge = edge,
            tracksObject = edgeModel,
        });

        BuildVertices();
    }

    private static Vector3 CalculateOverExtendedVertex(Vector3 start, Vector3 end, float overextendDistance)
    {
        var offset = end - start;
        return end + offset.normalized * overextendDistance;
    }

    private void UpdateLastEdgeEndPoint(Vector3 newEndPoint)
    {
        if (edges.Count > 0)
        {
            edges[edges.Count - 1].tracksObject.GetComponentInChildren<TrackModelGeneratorComponent>().SetEnd(newEndPoint);
        }
    }

    public Edge LastEdge()
    {
        return edges[edges.Count - 1].edge;
    }

    public Vector3 LastVertex()
    {
        return vertices[vertices.Count - 1];
    }

    public bool CanConnect(Edge edge)
    {
        if (!IsValidPath())
        {
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

    public bool CanSplit(Edge splitEdge)
    {
        if (!IsComplete())
        {
            return false;
        }

        int i;
        var foundEdge = FindDirectionalEdge(splitEdge, out i);
        if (foundEdge == null)
        {
            return false;
        }

        if (edges[0].Equals(foundEdge) || edges[edges.Count].Equals(foundEdge))
        {
            return false;
        }

        if (TrainIsOnEdge(foundEdge.edge))
        {
            return false;
        }

        return true;
    }

    public bool CanDeleteEdge(Edge edge)
    {
        if (edges.Count == 1)
        {
            // can't delete the last edge
            return false;
        }
        if (train != null && trainDistance > TotalTrackLength() - edge.length)
        {
            // can't delete edge if the train is on it
            return false;
        }
        var lastEdge = LastEdge();
        return lastEdge.NonDirectional().Equals(edge);
    }

    /// <summary>
    /// Splits the path at an edge and returns the new path. The split edge is deleted.
    /// </summary>
    public VertexPath Split(Edge splitEdge)
    {
        Debug.Assert(CanSplit(splitEdge));
        int edgeIndex;
        var foundEdge = FindDirectionalEdge(splitEdge, out edgeIndex);

        var vertexPath = Instantiate(net.vertexPathPrefab, transform);
        vertexPath.Init(net, LastVertex(), LastEdge());

        for (int i = edges.Count - 2; i > edgeIndex; i--)
        {
            vertexPath.AddEdge(edges[i].edge);
            DestroyEdge(edges[i]);
        }

        DestroyEdge(foundEdge);
        GameObject.Destroy(train);
        trainRunning = false;

        return vertexPath;
    }

    private EdgeAndInstanceData FindDirectionalEdge(Edge nonDirEdge, out int index)
    {
        Debug.Assert(nonDirEdge.direction == Edge.Direction.NONE);
        for (int i = 0; i < edges.Count; i++)
        {
            if (edges[i].edge.NonDirectional().Equals(nonDirEdge))
            {
                index = i;
                return edges[i];
            }
        }
        index = -1;
        return null;
    }

    private bool TrainIsOnEdge(Edge onEdge)
    {
        float len = 0;
        foreach (var edge in edges)
        {
            len += edge.edge.length;
            if (trainDistance < len)
            {
                return edge.edge == onEdge;
            }
        }
        Debug.LogWarning("didn't find edge?");
        return false;
    }

    public bool DeleteEdge(Edge edge)
    {
        if (!CanDeleteEdge(edge))
        {
            return false;
        }

        DestroyEdge(FindDirectionalEdge(edge, out var i));
        return true;
    }

    private void DestroyEdge(EdgeAndInstanceData edgeData)
    {
        GameObject.Destroy(edgeData.tracksObject);
        edges.Remove(edgeData);
        BuildVertices();
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
        AddEdge(directionalEdge);
        return true;
    }

    public void Join(VertexPath path)
    {
        path.vertices.Reverse();
        path.edges.Reverse();
        vertices.AddRange(path.vertices);
        foreach (var edgeData in path.edges)
        {
            AddEdge(edgeData.edge.DirectionalFrom(LastEdge().toVertex));
        }
    }

    private float TotalTrackLength()
    {
        float len = 0;
        foreach (var edgeData in edges)
        {
            len += edgeData.edge.length;
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
            Gizmos.DrawSphere(vertex, net.travelerScale);
        }
        Gizmos.color = Color.blue;
        foreach (var edgeData in edges)
        {
            var edge = edgeData.edge;
            Gizmos.DrawLine(edge.left, edge.right);
        }
    }

    private void SpawnTrain()
    {
        train = GameObject.Instantiate(net.trainPrefab, transform);
        train.transform.localScale *= net.travelerScale;
        train.transform.position = vertices[0];
    }

    public void StartMoving()
    {
        SpawnTrain();
        train.transform.position = vertices[0];
        trainDistance = 0;
        trainRunning = true;
        trainRunningLeft = false;
        StartCoroutine(MoveCoroutine());
    }

    private IEnumerator MoveCoroutine()
    {
        while (trainRunning)
        {
            if (!IsValidPath())
            {
                yield return null;
            }

            if (trainRunningLeft)
            {
                trainDistance -= net.moveSpeed * Time.deltaTime;
            }
            else
            {
                trainDistance += net.moveSpeed * Time.deltaTime;
            }

            float totalTrackLength = TotalTrackLength();

            if (trainRunningLeft && trainDistance < 0)
            {
                trainRunningLeft = false;
                trainDistance = 0;
            }
            else if (!trainRunningLeft && trainDistance > totalTrackLength)
            {
                trainRunningLeft = true;
                trainDistance = totalTrackLength;
                CompletedTrip();
            }

            MoveTraveler(trainDistance);
            yield return null;
        }
    }

    private void MoveTraveler(float distance)
    {
        float edgeDistance = distance;
        Edge foundEdge = null;
        Vector3 movePos = Vector3.zero;

        Assert.AreNotEqual(vertices.Count, 0);
        foreach (var edgeData in edges)
        {
            var edge = edgeData.edge;
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
    }
}
