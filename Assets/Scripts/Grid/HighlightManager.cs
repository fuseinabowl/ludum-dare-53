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

    private List<GameObject> highlights = new List<GameObject>();

    private void Awake()
    {
        Debug.Log("initialising");
        CreateAvailableEdgeHighlights();
        network.onAvailableEdgesChanged += OnAvailableEdgesChanged;
    }

    private void OnAvailableEdgesChanged()
    {
        DestroyExistingEdgeHighlights();
        CreateAvailableEdgeHighlights();
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
        Debug.Log("Creating available edges");
        var availableEdges = network.AllConnectableEdges();
        foreach (var edge in availableEdges)
        {
            Debug.Log("Creating an edge");
            var newHighlight = GameObject.Instantiate(highlightPrefab);

            newHighlight.transform.localScale = new Vector3(
                1f,
                1f,
                edge.length
            );
            newHighlight.transform.position = edge.middle;
            newHighlight.transform.rotation = Quaternion.LookRotation(edge.extent, Vector3.up);

            highlights.Add(newHighlight);
        }
    }
}
