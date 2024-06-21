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
            // Don't play the track-complete jingle because it would play over the
            // game-over jingle.
            SFX.singleton.trackComplete.Play();
        }
        RunGameOverAnimation();
    }

    private bool IsValidPath()
    {
        return vertices != null & vertices.Count >= 2;
    }

    private void OnDrawGizmos()
    {
        var gizmos = GetComponent<GizmoRenderer>();

        foreach (var vertex in vertices)
        {
            gizmos.DrawSphere(vertex, net.travelerScale);
        }

        foreach (var edgeData in edges)
        {
            var edge = edgeData.edge;
            gizmos.DrawLine(
                edge.left,
                edge.right,
                arrow: true,
                variant: GizmoRenderer.Variant.SECONDARY
            );
        }
    }

    private void RunGameOverAnimation()
    {
        // This method skips the first/last edge because each train station takes up 2 edges.
        for (
            int i = 0, tapsRemaining = edges.Count - 1;
            tapsRemaining > 0;
            i += net.trackCompletedFmodMaxSpawn
        )
        {
            int taps = Mathf.Min(net.trackCompletedFmodMaxSpawn, tapsRemaining);
            StartCoroutine(PlayTrackCompleteKnocks(net.trackCompletedAnimPeriod * i, taps));
            tapsRemaining -= taps;
        }

        StartCoroutine(PlayTrackCompleteFinalKnock(net.trackCompletedAnimPeriod * (edges.Count - 2)));

        for (int i = 0; i < edges.Count - 2; i++)
        {
            StartCoroutine(AnimateTrack(i * net.trackCompletedAnimPeriod, edges[i + 1]));
        }
    }

    private IEnumerator PlayTrackCompleteKnocks(float delay, int spawnTotal)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }
        SFX.PlayOneShot(
            gameObject,
            SFX.singleton.trackCompleteKnocks,
            "SpawnRate",
            0.1f / net.trackCompletedAnimPeriod,
            "SpawnTotal",
            spawnTotal
        );
    }

    private IEnumerator PlayTrackCompleteFinalKnock(float delay)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }
        SFX.PlayOneShot(gameObject, SFX.singleton.trackCompleteFinalKnock);
    }

    private IEnumerator AnimateTrack(float delay, EdgeAndInstanceData edge)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }
        edge.tracksObject.GetComponent<TransformAnimator>().Animate();
    }
}
