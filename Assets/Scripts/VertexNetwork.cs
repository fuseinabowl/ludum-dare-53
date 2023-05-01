using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexNetwork : MonoBehaviour
{
    [Header("Config")]
    public float travelerScale = 1f;
    public bool triIsPassable = false;
    [Range(0, 1)]
    public float trackCompletedAnimPeriod = 0.1f;

    [Header("Objects")]
    public VertexPath vertexPathPrefab;
    public GameObject edgeModelPrefab;
    public GameObject trainPrefab;

    [Header("Gizmos")]
    public bool gizmoVertices;
    public bool gizmoEdges;

    [Header("Edge config")]
    public float moveSpeed = 1f;

    [HideInInspector]
    public HashSet<Vector3> rootVectors = new HashSet<Vector3>();

    public delegate void UpdateEvent();
    public UpdateEvent onAvailableEdgesChanged;

    private EdgeGraph edgeGraph = null;
    private List<VertexPath> vertexPaths = new List<VertexPath>();
    public IReadOnlyList<VertexPath> VertexPaths => vertexPaths;
    private Edge closestEdge = null;
    private bool canPlaceClosestEdge = false;
    private bool canDeleteClosestEdge = false;
    private bool canSplitClosestEdge = false;
    private Vector3 mouseHit;
    private List<Station> stations = new List<Station>();
    private List<Station> pendingStations = new List<Station>();
    private List<Vector3> pendingRemoveMidpointEdges = new List<Vector3>();

    public void SetEdgeGraph(EdgeGraph eg)
    {
        edgeGraph = eg;
        foreach (var pendingStation in pendingStations)
        {
            InitStation(pendingStation);
        }
        pendingStations.Clear();
        foreach (var mid in pendingRemoveMidpointEdges) {
            edgeGraph.RemoveMidpointEdges(mid);
        }
        pendingRemoveMidpointEdges.Clear();
    }

    public void AddStation(Station station)
    {
        if (edgeGraph == null)
        {
            pendingStations.Add(station);
        }
        else
        {
            InitStation(station);
        }
    }

    private void InitStation(Station station)
    {
        var rootVertex = edgeGraph.ClosestVertex(station.transform.position);
        if (station.front == null)
        {
            Debug.LogWarning("station has no front object, cannot create path");
            return;
        }

        onAvailableEdgesChanged?.Invoke();
        var frontVertex = edgeGraph.ClosestVertex(station.front.transform.position);
        rootVectors.Add(rootVertex);
        var vertexPath = Instantiate(vertexPathPrefab, transform);
        vertexPaths.Add(vertexPath);
        vertexPath.Init(this, rootVertex, edgeGraph.FindEdge(frontVertex, rootVertex));
        stations.Add(station);
        station.rootVertex = rootVertex;
    }

    private void Update()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        mouseHit = Physics.Raycast(ray, out hit) ? hit.point : Vector3.zero;
        closestEdge = edgeGraph.ClosestEdge(mouseHit);
        canPlaceClosestEdge = CanPlaceEdge(closestEdge);
        canDeleteClosestEdge = CanDeleteEdge(closestEdge);
        canSplitClosestEdge = CanSplitEdge(closestEdge);

        if (Input.GetMouseButtonDown(0) && canPlaceClosestEdge)
        {
            PlaceEdge(closestEdge);
        }
        else if (Input.GetMouseButtonDown(1) && canDeleteClosestEdge)
        {
            DeleteEdge(closestEdge);
        }
        else if (Input.GetMouseButtonDown(1) && canSplitClosestEdge)
        {
            SplitEdge(closestEdge);
        }
    }

    private void PlaceEdge(Edge edge)
    {
        VertexPath connectPath = null;
        VertexPath joinPath = null;

        foreach (var path in vertexPaths)
        {
            if (path.CanConnect(edge))
            {
                if (connectPath == null)
                {
                    connectPath = path;
                }
                else if (joinPath == null)
                {
                    joinPath = path;
                }
            }
        }

        if (connectPath == null)
        {
            Debug.LogError("couldn't find a path to connect the edge to!!!!!!!");
            return;
        }

        connectPath.Connect(edge);

        if (joinPath)
        {
            connectPath.Join(joinPath);
            vertexPaths.Remove(joinPath);
            GameObject.Destroy(joinPath);
        }

        if (connectPath.IsComplete())
        {
            connectPath.NotifyCompleted();
        }

        onAvailableEdgesChanged?.Invoke();
    }

    public List<Edge> ConnectableEdges(VertexPath path)
    {
        var lastVertex = path.LastVertex();
        var lastEdge = path.LastEdge();
        var adjacentEdges = edgeGraph.AdjacentEdges(lastVertex);

        // Sort the edges to be continuous around the last vertex.
        Comparison<Edge> compare = (edgeA, edgeB) =>
        {
            var directionalA = edgeA.DirectionalFrom(lastVertex);
            var directionalB = edgeB.DirectionalFrom(lastVertex);
            var angleA = Vector3.SignedAngle(directionalA.extent, lastEdge.extent, Vector3.up);
            var angleB = Vector3.SignedAngle(directionalB.extent, lastEdge.extent, Vector3.up);
            return angleA.CompareTo(angleB);
        };
        adjacentEdges.Sort(compare);

        // The connectable edges are those that aren't adjacent to the graph's last edge.
        // Convert the last edge to non-directional since adjacentEdges are non-directional
        // and we want Equals to work.
        var lastEdgeNonDirectional = lastEdge.NonDirectional();
        var connectableEdges = new List<Edge>();

        for (int i = 0; i < adjacentEdges.Count; i++)
        {
            if (!lastEdgeNonDirectional.Equals(adjacentEdges[i]))
            {
                if (
                    (adjacentEdges.Count == 3 && triIsPassable)
                    || (
                        !lastEdgeNonDirectional.Equals(Lists.Circ(adjacentEdges, i - 1))
                        && !lastEdgeNonDirectional.Equals(Lists.Circ(adjacentEdges, i + 1))
                    )
                )
                {
                    connectableEdges.Add(adjacentEdges[i]);
                }
            }
        }

        return connectableEdges;
    }

    /// <summary>
    /// Returns the set of all non-directional edges that can be connected to on any path,
    ///
    /// </summary>
    /// <returns></returns>
    public HashSet<Edge> AllConnectableEdges()
    {
        var all = new HashSet<Edge>();

        foreach (var path in vertexPaths)
        {
            foreach (var edge in ConnectableEdges(path))
            {
                Debug.Assert(edge.direction == Edge.Direction.NONE);
                all.Add(edge);
            }
        }

        return all;
    }

    private bool CanPlaceEdge(Edge edge)
    {
        foreach (var path in vertexPaths)
        {
            if (path.HasInternalVertexOnEdge(edge))
            {
                return false;
            }
        }

        VertexPath connectPath = null;
        VertexPath joinPath = null;

        foreach (var path in vertexPaths)
        {
            if (path.CompletesLoop(edge))
            {
                return false;
            }
            if (path.CanConnect(edge))
            {
                if (connectPath == null)
                {
                    connectPath = path;
                }
                else if (joinPath == null)
                {
                    joinPath = path;
                }
                else
                {
                    // too many connections
                    return false;
                }
            }
        }

        if (connectPath)
        {
            if (joinPath)
            {
                return FindStartStation(connectPath).type != FindStartStation(joinPath).type;
            }
            return true;
        }

        return false;
    }

    private Station FindStartStation(VertexPath path)
    {
        foreach (var station in stations)
        {
            if (station.rootVertex == path.vertices[0])
            {
                return station;
            }
        }
        return null;
    }

    private bool CanDeleteEdge(Edge edge)
    {
        foreach (var path in vertexPaths)
        {
            if (path.CanDeleteEdge(edge))
            {
                return true;
            }
        }
        return false;
    }

    private void DeleteEdge(Edge edge)
    {
        foreach (var path in vertexPaths)
        {
            if (path.DeleteEdge(edge))
            {
                onAvailableEdgesChanged?.Invoke();
                return;
            }
        }
    }

    private bool CanSplitEdge(Edge edge)
    {
        foreach (var path in vertexPaths)
        {
            if (path.CanSplit(edge))
            {
                return true;
            }
        }
        return false;
    }

    private void SplitEdge(Edge edge)
    {
        foreach (var path in vertexPaths)
        {
            var vertexPath = path.Split(edge);
            if (vertexPath)
            {
                vertexPaths.Add(vertexPath);
                return;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (edgeGraph != null)
        {
            if (gizmoVertices)
            {
                foreach (var vertex in edgeGraph.vertices)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(vertex, travelerScale / 2f);
                }
            }

            if (closestEdge != null)
            {
                Gizmos.color = canPlaceClosestEdge
                    ? Color.green
                    : canDeleteClosestEdge
                        ? Color.yellow
                        : Color.red;
                Gizmos.DrawLine(closestEdge.left, closestEdge.right);
            }

            foreach (var edge in AllConnectableEdges())
            {
                if (edge != closestEdge)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(edge.left, edge.right);
                }
            }

            if (gizmoEdges)
            {
                foreach (var edge in edgeGraph.edges)
                {
                    if (edge != closestEdge)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(edge.left, edge.right);
                    }
                }
            }
        }

        if (mouseHit != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                mouseHit + travelerScale / 10f * Vector3.left,
                mouseHit + travelerScale / 10f * Vector3.right
            );
            Gizmos.DrawLine(
                mouseHit + travelerScale / 10f * Vector3.back,
                mouseHit + travelerScale / 10f * Vector3.forward
            );
        }
    }

    public void RemoveMidpointEdges(Vector3 pos)
    {
        if (edgeGraph != null) {
            edgeGraph.RemoveMidpointEdges(pos);
        } else {
            pendingRemoveMidpointEdges.Add(pos);
        }
    }
}
