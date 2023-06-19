using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class VertexNetwork : MonoBehaviour
{
    public static VertexNetwork singleton
    {
        get { return SingletonProvider.Get<VertexNetwork>(); }
        private set { }
    }

    [Header("Config")]
    public float travelerScale = 1f;
    public bool triIsPassable = false;

    [Range(0, 1)]
    public float trackCompletedAnimPeriod = 0.1f;

    [Range(1, 32)]
    public int trackCompletedFmodMaxSpawn = 32;

    [Header("Objects")]
    public VertexPath vertexPathPrefab;
    public GameObject edgeModelPrefab;
    public GameObject trainPrefab;

    [Header("Edge config")]
    public float moveSpeed = 1f;

    public delegate void UpdateEvent();
    public UpdateEvent onAvailableEdgesChanged;

    private EdgeGraph edgeGraph = null;
    private List<VertexPath> vertexPaths = new List<VertexPath>();
    public IReadOnlyList<VertexPath> VertexPaths => vertexPaths;
    private List<FarmStation> farms = new List<FarmStation>();
    private List<TownStation> towns = new List<TownStation>();

    public void SetEdgeGraph(EdgeGraph eg)
    {
        edgeGraph = eg;
    }

    public void AddFarmStation(FarmStation station)
    {
        Assert.IsNotNull(edgeGraph);
        InitFarmStation(station);
    }

    private void InitFarmStation(FarmStation station)
    {
        if (station.front == null)
        {
            Debug.LogWarning("station has no front object, cannot create path");
            return;
        }

        var rootVertex = edgeGraph.ClosestVertex(station.transform.position);

        onAvailableEdgesChanged?.Invoke();
        var frontVertex = edgeGraph.ClosestVertex(station.front.transform.position);
        var vertexPath = Instantiate(vertexPathPrefab, transform);
        vertexPaths.Add(vertexPath);
        vertexPath.Init(this, rootVertex, edgeGraph.FindEdge(frontVertex, rootVertex));
        farms.Add(station);
        station.rootVertex = rootVertex;
    }

    public void AddTownStation(TownStation station)
    {
        Assert.IsNotNull(edgeGraph);
        InitTownStation(station);
    }

    private void InitTownStation(TownStation station)
    {
        if (station.front == null)
        {
            Debug.LogWarning("station has no front object, cannot create path");
            return;
        }

        var rootVertex = edgeGraph.ClosestVertex(station.transform.position);
        var frontVertex = edgeGraph.ClosestVertex(station.front.transform.position);
        BlockAllEdgesExceptEdgeToward(rootVertex, frontVertex);

        towns.Add(station);
        station.rootVertex = rootVertex;
    }

    public bool IsTownAt(Vector3 vertex)
    {
        return towns.Any(town => town.rootVertex == vertex);
    }

    public TownStation GetTownAt(Vector3 vertex)
    {
        return towns.First(town => town.rootVertex == vertex);
    }

    private void BlockAllEdgesExceptEdgeToward(Vector3 fromVertex, Vector3 toVertex)
    {
        foreach (
            var edge in edgeGraph.edges.Where(edge =>
            {
                var matchingDirectionResult =
                    edge.fromVertex == fromVertex && edge.toVertex != toVertex;
                var inverseDirectionResult =
                    edge.toVertex == fromVertex && edge.fromVertex != toVertex;

                return matchingDirectionResult || inverseDirectionResult;
            })
        )
        {
            edge.blocked = true;
        }
    }

    // private void OnInputMouseButtonDown(SimpleInputManager.MouseEvent e)
    // {
    //     if (e.left)
    //     {
    //         var closestEdge = edgeGraph.ClosestEdge(e.raycastHit.Value.point);
    //         if (CanPlaceEdge(closestEdge))
    //         {
    //             PlaceEdge(closestEdge);
    //         }
    //         else if (CanDeleteEdge(closestEdge))
    //         {
    //             DeleteEdge(closestEdge);
    //         }
    //     }
    // }

    private void OnMouseHitDown(MouseHitTarget.Event e)
    {
        if (e.left)
        {
            var closestEdge = edgeGraph.ClosestEdge(e.raycastHit.Value.point);
            if (CanPlaceEdge(closestEdge))
            {
                PlaceEdge(closestEdge);
            }
            else if (CanDeleteEdge(closestEdge))
            {
                DeleteEdge(closestEdge);
            }
        }
    }

    private void PlaceEdge(Edge edge)
    {
        VertexPath connectPath = null;

        foreach (var path in vertexPaths)
        {
            if (path.CanConnect(edge))
            {
                if (connectPath == null)
                {
                    connectPath = path;
                }
            }
        }

        if (connectPath == null)
        {
            Debug.LogError("couldn't find a path to connect the edge to");
            return;
        }

        connectPath.Connect(edge);

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
    /// Returns the set of all non-directional edges that can be connected to on any path.
    /// </summary>
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
                else
                {
                    // too many connections
                    return false;
                }
            }
        }

        if (connectPath)
        {
            return true;
        }

        return false;
    }

    private FarmStation FindStartStation(VertexPath path)
    {
        foreach (var farm in farms)
        {
            if (farm.rootVertex == path.vertices[0])
            {
                return farm;
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

    private void OnDrawGizmos()
    {
        if (edgeGraph != null)
        {
            var gizmos = GetComponent<GizmoRenderer>();
            var connectableEdges = AllConnectableEdges();

            foreach (var vertex in edgeGraph.vertices)
            {
                // gizmos.DrawSphere(vertex, travelerScale / 3f);
            }

            foreach (var edge in edgeGraph.edges)
            {
                if (!connectableEdges.Contains(edge))
                {
                    gizmos.DrawLine(edge.left, edge.right, arrow: true);
                }
            }

            foreach (var edge in connectableEdges)
            {
                gizmos.DrawLine(edge.left, edge.right, GizmoRenderer.Variant.TERTIARY, arrow: true);
            }
        }
    }

    public void RemoveClosestVertex(Vector3 pos)
    {
        Assert.IsNotNull(edgeGraph);
        edgeGraph.RemoveClosestVertex(pos);
    }

    public void BlockClosestVertex(Vector3 pos)
    {
        Assert.IsNotNull(edgeGraph);
        edgeGraph.BlockClosestVertex(pos);
    }
}
