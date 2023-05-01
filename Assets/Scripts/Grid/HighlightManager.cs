using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightManager : MonoBehaviour
{
    [SerializeField]
    private IrregularGrid grid;
    [SerializeField]
    private VertexNetwork network;

    [SerializeField]
    private GameObject highlightPrefab;
    [SerializeField]
    private GameObject destroyHighlightPrefab;

    private List<GameObject> highlights = new List<GameObject>();

    private void Awake()
    {
        CreateAvailableEdgeHighlights();
        CreateDestroyableEdgeHighlights();
        network.onAvailableEdgesChanged += OnAvailableEdgesChanged;
    }

    private void OnAvailableEdgesChanged()
    {
        DestroyExistingEdgeHighlights();
        CreateAvailableEdgeHighlights();
        CreateDestroyableEdgeHighlights();
    }

    private void DestroyExistingEdgeHighlights()
    {
        foreach (var highlight in highlights)
        {
            GameObject.Destroy(highlight);
        }
        highlights.Clear();
    }

    private void CreateAvailableEdgeHighlights()
    {
        foreach (var path in network.VertexPaths)
        {
            foreach (var edge in network.ConnectableEdges(path))
            {
                if (path.CanConnect(edge))
                {
                    var newHighlight = GameObject.Instantiate(highlightPrefab);

                    newHighlight.transform.localScale = new Vector3(
                        1f,
                        1f,
                        edge.length
                    );
                    newHighlight.transform.position = edge.middle;
                    newHighlight.transform.rotation = Quaternion.LookRotation(path.LastVertex() - edge.middle, Vector3.up);

                    highlights.Add(newHighlight);
                }
            }
        }
    }

    private void CreateDestroyableEdgeHighlights()
    {
        foreach (var path in network.VertexPaths)
        {
            var edge = path.LastEdge();
            if (path.CanDeleteEdge(edge.NonDirectional()))
            {
                var newHighlight = GameObject.Instantiate(destroyHighlightPrefab);

                newHighlight.transform.localScale = new Vector3(
                    1f,
                    1f,
                    edge.length
                );
                newHighlight.transform.position = edge.middle;
                newHighlight.transform.rotation = Quaternion.LookRotation(path.LastVertex() - edge.middle, Vector3.up);

                highlights.Add(newHighlight);
            }
        }
    }
}
