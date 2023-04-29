using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public GameObject[] vertices = new GameObject[]{};

    [Header("Movement")]
    public float moveSpeed = 1f;

    private List<Edge> edges = new List<Edge>();
    private float totalTrackLength = 0;
    private float distance;
    private bool running;
    private bool runningLeft;

    private void Start() {
        CreateEdges();

        foreach (var edge in edges) {
            totalTrackLength += edge.length;
        }

        MoveToStart();
        StartMoving();
    }

    private void CreateEdges() {
        for (int i = 1; i < vertices.Length; i++) {
            edges.Add(new Edge(vertices[i-1].transform.position, vertices[i].transform.position));
        }
    }

    private void MoveToStart() {
        if (!CanMove()) {
            Debug.LogError("Must specify a traveler node and at least one vertex!");
            return;
        }
        traveler.transform.position = vertices[0].transform.position;
        distance = 0;
    }

    private bool CanMove() {
        return traveler != null && vertices.Length > 0;
    }

    private void OnDrawGizmos() {
        foreach (var edge in edges) {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(edge.left, edge.right);
        }
    }

    private void StartMoving() {
        running = true;
        runningLeft = false;
        StartCoroutine(MoveCoroutine());
    }    

    private IEnumerator MoveCoroutine() {
        if (!CanMove()) {
            yield return null;
        }

        while (running) {
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
            movePos = vertices[0].transform.position;
        }

        traveler.transform.position = movePos;
    }
}
