using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class VertexPath : MonoBehaviour
{
    private class Edge {
        public Vector3 left;
        public Vector3 right;
        public float length;

        public Edge(Vector3 left, Vector3 right) {
            this.left = left;
            this.right = right;
            length = Vector3.Distance(left, right);
        }
    }

    [Header("Nodes")]
    public GameObject traveler;
    public List<GameObject> initialVertices = new List<GameObject>();

    [Header("Movement")]
    public float moveSpeed = 1f;

    private int initialVerticesCountInEditor = -1;
    private List<Vector3> vertices = new List<Vector3>();
    private List<Edge> edges = new List<Edge>();
    private float totalTrackLength = 0;
    private float distance;
    private bool running;
    private bool runningLeft;

    public void AddLeftVertex(Vector3 vertex) {
        vertices.Insert(0, Vectors.Z0(vertex));
        CreateEdges();
        distance += edges[0].length;
    }

    public void AddRightVertex(Vector3 vertex) {
        vertices.Add(Vectors.Z0(vertex));
        CreateEdges();
    }

    public void RemoveLeftVertex() {
        if (!IsValidPath()) {
            return;
        }
        distance -= edges[0].length;
        if (distance < 0) {
            // the game should probably prevent this from happening (the track that the train is on was deleted)
            distance = 0;
        }
        vertices.RemoveAt(0);
        CreateEdges();
    }

    public void RemoveRightVertex() {
        if (!IsValidPath()) {
            return;
        }
        vertices.RemoveAt(vertices.Count - 1);
        CreateEdges();
        if (distance > totalTrackLength) {
            // the game should probably prevent this from happening (the track that the train is on was deleted)
            distance = totalTrackLength;
        }
    }

    private void Start() {
        InitVertices();
        CreateEdges();

        if (Application.isPlaying && IsValidPath()) {
            MoveToStart();
            StartMoving();
        }
    }


    private void InitVertices() {
        vertices = new List<Vector3>();

        foreach (var vertexObj in initialVertices) {
            if (vertexObj != null) {
                vertices.Add(Vectors.Z0(vertexObj.transform.position));
            }
        }

        foreach (var edge in edges) {
            totalTrackLength += edge.length;
        }
    }

    private void CreateEdges() {
        edges = new List<Edge>();
        totalTrackLength = 0;

        for (int i = 1; i < vertices.Count; i++) {
            var edge = new Edge(vertices[i-1], vertices[i]);
            edges.Add(edge);
            totalTrackLength += edge.length;
        }
    }

    private void MoveToStart() {
        traveler.transform.position = vertices[0];
        distance = 0;
    }

    private bool IsValidPath() {
        return traveler != null && vertices != null & vertices.Count >= 2;
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.green;
        foreach (var vertex in vertices) {
            Gizmos.DrawSphere(vertex, 0.3f);
        }

        Gizmos.color = Color.blue;
        foreach (var edge in edges) {
            Gizmos.DrawLine(edge.left, edge.right);
        }
    }

    private void StartMoving() {
        running = true;
        runningLeft = false;
        StartCoroutine(MoveCoroutine());
    }    

    private IEnumerator MoveCoroutine() {
        while (running) {
            if (!IsValidPath()) {
                yield return null;
            }

            if (runningLeft) {
                distance -= moveSpeed * Time.deltaTime;
            } else {
                distance += moveSpeed * Time.deltaTime;
            }

            if (runningLeft && distance < 0) {
                runningLeft = false;
                distance = 0;
            } else if (!runningLeft && distance > totalTrackLength) {
                runningLeft = true;
                distance = totalTrackLength;
            }

            MoveTraveler(distance);
            yield return null;
        }
    }

    private void MoveTraveler(float distance) {
        float edgeDistance = distance;
        bool found = false;
        Vector3 movePos = Vector3.zero;

        foreach (var edge in edges) {
            if (edgeDistance <= edge.length) {
                movePos = Vector3.Lerp(edge.left, edge.right, edgeDistance / edge.length);
                found = true;
                break;
            }
            edgeDistance -= edge.length;
        }

        if (!found) {
            Debug.LogWarningFormat("Didn't find position at {0}", distance);
            movePos = vertices[0];
        }

        traveler.transform.position = movePos;
    }

    private void Update() {
        if (!Application.isPlaying) {
            return;
        }

        Vector3 mouseWorldPoint = Vectors.Z0(Camera.main.ScreenToWorldPoint(Input.mousePosition));

        if (Input.GetMouseButtonDown(0)) {
            if (Input.GetKey(KeyCode.LeftControl)) {
                RemoveRightVertex();
            } else {
                AddRightVertex(mouseWorldPoint);
            }
        }

        if (Input.GetMouseButtonDown(1)) {
            if (Input.GetKey(KeyCode.LeftControl)) {
                RemoveLeftVertex();
            } else {
                AddLeftVertex(mouseWorldPoint);
            }
        }
    }
}
