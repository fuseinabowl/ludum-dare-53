using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexNetwork : MonoBehaviour
{
    public List<GameObject> roots = new List<GameObject>();
    public float travelerScale = 1f;
    public VertexPath vertexPathPrefab;
    public float minEdgeAngle = 90f;

    [Header("Gizmos")]
    public bool gizmoEdges;

    [Header("Edge config")]
    public float moveSpeed = 1f;

    [HideInInspector]
    public HashSet<Vector3> rootVectors = null;

    private EconomyController economy;
    private EdgeGraph edgeGraph = null;
    private List<VertexPath> vertexPaths = new List<VertexPath>();
    private Edge closestEdge = null;
    private bool canPlaceClosestEdge = false;
    private bool canDeleteClosestEdge = false;
    private Vector3 mouseHit;

    public void SetEdgeGraph(EdgeGraph eg)
    {
        edgeGraph = eg;
        rootVectors = new HashSet<Vector3>();
        foreach (var root in roots)
        {
            rootVectors.Add(edgeGraph.ClosestVertex(root.transform.position));
        }
    }

    private void Start() {
        economy = SingletonProvider.Get<EconomyController>();
    }

    private void Update()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        mouseHit = Physics.Raycast(ray, out hit) ? hit.point : Vector3.zero;
        closestEdge = edgeGraph.ClosestEdge(mouseHit);
        canPlaceClosestEdge = CanPlaceEdge(closestEdge);
        canDeleteClosestEdge = CanDeleteEdge(closestEdge);

        if (Input.GetMouseButtonDown(0) && canPlaceClosestEdge)
        {
            if (economy.CanBuyTrack()) {
                economy.BuyAndPlaceTrack();
                PlaceEdge(closestEdge);
            } else {
                economy.CannotBuy();
            }
        }
        else if (Input.GetMouseButtonDown(1) && canDeleteClosestEdge)
        {
            DeleteEdge(closestEdge);
        }
    }

    private void PlaceEdge(Edge edge)
    {
        Vector3 root;
        bool isOnRoot = IsOnRoot(edge, out root);

        foreach (var path in vertexPaths)
        {
            if (path.CanConnect(edge))
            {
                if (isOnRoot)
                {
                    // either:
                    // (1) player is connecting one root to another, in which case the path should be extended.
                    // (2) the player is starting another path from the root, in which case a new path should be created.
                    if (path.EndsWith(root))
                    {
                        break;
                    }
                }
                path.Connect(edge);
                return;
            }
        }

        Debug.Assert(isOnRoot);
        var vertexPath = Instantiate(vertexPathPrefab, transform);
        vertexPaths.Add(vertexPath);
        vertexPath.Init(this, root, edge, travelerScale, minEdgeAngle);
        vertexPath.StartMoving();
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

        int connectablePathCount = 0;

        foreach (var path in vertexPaths)
        {
            if (path.CompletesLoop(edge))
            {
                return false;
            }
            if (path.CanConnect(edge))
            {
                connectablePathCount++;
            }
        }

        if (connectablePathCount == 1)
        {
            return true;
        }

        if (connectablePathCount > 1)
        {
            return false;
        }

        // todo: cannot form loops to deliver to the same root

        Vector3 root;
        return IsOnRoot(edge, out root);
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
                return;
            }
        }
    }

    private bool IsOnRoot(Edge edge, out Vector3 root)
    {
        foreach (var rootVector in rootVectors)
        {
            if (edge.left == rootVector || edge.right == rootVector)
            {
                root = rootVector;
                return true;
            }
        }
        root = Vector3.zero;
        return false;
    }

    private void OnDrawGizmos()
    {
        if (edgeGraph != null)
        {
            foreach (var vertex in edgeGraph.vertices)
            {
                Gizmos.color = rootVectors.Contains(vertex) ? Color.red : Color.blue;
                Gizmos.DrawSphere(vertex, travelerScale / 2f);
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
}
