using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexNetwork : MonoBehaviour
{
    private EdgeGraph edgeGraph = null;

    private Vector3 mouseHit;

    public void SetEdgeGraph(EdgeGraph edgeGraph)
    {
        this.edgeGraph = edgeGraph;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                mouseHit = hit.point;
            }
            else
            {
                mouseHit = Vector3.zero;
            }
        }
    }


    private void OnDrawGizmos()
    {
        if (edgeGraph != null)
        {
            foreach (var vertex in edgeGraph.vertices)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(vertex, 0.01f);
            }

            var closestEdge = edgeGraph.ClosestEdge(mouseHit);

            foreach (var edge in edgeGraph.edges)
            {
                if (edge == closestEdge)
                {
                    Gizmos.color = Color.red;
                }
                else
                {
                    Gizmos.color = Color.green;
                }
                Gizmos.DrawLine(edge.left, edge.right);
            }
        }
    }

}
