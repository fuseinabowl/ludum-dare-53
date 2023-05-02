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
    public VertexNetwork Net => net;

    public delegate void TrackEvent();
    public TrackEvent fullTrackEstablished;
    public TrackEvent fullTrackBroken;

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
        edgeModel
            .GetComponentInChildren<TrackModelGeneratorComponent>()
            .SetPoints(edge.middle, edge.toVertex);

        UpdateLastEdgeEndPoint(edge.middle);
        edges.Add(new EdgeAndInstanceData { edge = edge, tracksObject = edgeModel, });

        BuildVertices();
    }

    private void UpdateLastEdgeEndPoint(Vector3 newEndPoint)
    {
        if (edges.Count > 0)
        {
            edges[edges.Count - 1].tracksObject
                .GetComponentInChildren<TrackModelGeneratorComponent>()
                .SetEnd(newEndPoint);
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

        if (edge.blocked)
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

    public bool CanDeleteEdge(Edge edge)
    {
        if (edges.Count == 1)
        {
            // can't delete the last edge
            return false;
        }

        var lastEdge = LastEdge();
        return lastEdge.NonDirectional().Equals(edge);
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

    public bool DeleteEdge(Edge edge)
    {
        if (!CanDeleteEdge(edge))
        {
            return false;
        }

        if (IsComplete())
        {
            var town = net.GetTownAt(LastEdge().toVertex);
            town.OnDisconnected();
            fullTrackBroken?.Invoke();
        }

        DestroyEdge(FindDirectionalEdge(edge, out var i));
        edges[edges.Count - 1].tracksObject
            .GetComponentInChildren<TrackModelGeneratorComponent>()
            .SetEndToIdle();
        SFX.Play(SFX.singleton.trackDeleted);

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
        SFX.Play(SFX.singleton.trackPlaced);
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

    public float TotalTrackLength()
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
        return IsValidPath() && net.IsTownAt(LastVertex());
    }

    public void NotifyCompleted()
    {
        var town = net.GetTownAt(LastEdge().toVertex);
        town.OnConnected();
        fullTrackEstablished?.Invoke();
        if (!GameController.singleton.gameOver)
        {
            StartCoroutine(TrackCompletedAnimation());
        }
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

    private IEnumerator TrackCompletedAnimation()
    {
        SFX.Play(
            SFX.singleton.trackComplete,
            "SpawnRate",
            0.1f / net.trackCompletedAnimPeriod,
            "SpawnTotal",
            edges.Count - 2
        );

        for (int i = 1; i < edges.Count - 1; i++)
        {
            var edge = edges[i];
            edge.tracksObject.GetComponent<TransformAnimator>().Animate();
            yield return new WaitForSeconds(net.trackCompletedAnimPeriod);
        }
    }
}
