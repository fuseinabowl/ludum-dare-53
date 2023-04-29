using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexNetwork : MonoBehaviour
{
    public List<GameObject> roots = new List<GameObject>();
    public float travelerScale = 1f;
    public VertexPath vertexPathPrefab;
    
    private EdgeGraph edgeGraph = null;
    private HashSet<Vector3> rootVectors = null;
    private List<VertexPath> vertexPaths = new List<VertexPath>();
    private Edge closestEdge = null;
    private bool canPlaceClosestEdge = false;
    private Vector3 mouseHit;

    public void SetEdgeGraph(EdgeGraph eg)
    {
        edgeGraph = eg;
        rootVectors = new HashSet<Vector3>();
        foreach (var root in roots) {
            rootVectors.Add(edgeGraph.ClosestVertex(root.transform.position));
        }
    }

    private void Update()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        mouseHit = Physics.Raycast(ray, out hit) ? hit.point : Vector3.zero;
        closestEdge = edgeGraph.ClosestEdge(mouseHit);
        canPlaceClosestEdge = CanPlaceEdge(closestEdge);

        if (canPlaceClosestEdge && Input.GetMouseButtonDown(0)) {
            PlaceEdge(closestEdge);
        }
    }

    private void PlaceEdge(Edge edge) {
        foreach (var path in vertexPaths) {
            if (path.CanConnect(edge)) {
                path.Connect(edge);
                return;
            }
        }
        
        Vector3 root;
        bool isOnRoot = IsOnRoot(edge, out root);
        Debug.Assert(isOnRoot);

        var vertexPath = Instantiate(vertexPathPrefab, transform);
        vertexPaths.Add(vertexPath);
        vertexPath.Init(root, edge, travelerScale);
        vertexPath.StartMoving();
    }

    private bool CanPlaceEdge(Edge edge) {
        bool connectsToPath = false;

        foreach (var path in vertexPaths) {
            if (path.ContainsEdge(edge)) {
                return false;
            }
            if (path.CanConnect(edge)) {
                connectsToPath = true;
            }
        }

        if (connectsToPath) {
            return true;
        }

        Vector3 root;
        return IsOnRoot(edge, out root);
    }

    private bool IsOnRoot(Edge edge, out Vector3 root) {
        foreach (var rootVector in rootVectors) {
            if (edge.left == rootVector || edge.right == rootVector) {
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

            if (closestEdge != null) {
                Gizmos.color = canPlaceClosestEdge ? Color.green : Color.red;
                Gizmos.DrawLine(closestEdge.left, closestEdge.right);
            }
        }

        if (mouseHit != Vector3.zero) {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(mouseHit + travelerScale / 10f * Vector3.left, mouseHit + travelerScale / 10f * Vector3.right);
            Gizmos.DrawLine(mouseHit + travelerScale / 10f * Vector3.back, mouseHit + travelerScale / 10f * Vector3.forward);
        }
    }
}
