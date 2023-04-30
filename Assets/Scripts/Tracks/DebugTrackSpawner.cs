using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class DebugTrackSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject start;
    [SerializeField]
    private GameObject control;
    [SerializeField]
    private GameObject end;

    [SerializeField]
    private Mesh trackTemplate;

    [SerializeField]
    private MeshFilter outputFilter;

    [SerializeField]
    [Min(1)]
    private int segments;

    private void Update()
    {
        if (AllValid())
        {
            GenerateTrack();
        }
    }

    private bool AllValid()
    {
        return start != null
            && control != null
            && end != null
            && trackTemplate != null
            && outputFilter != null;
    }

    private void GenerateTrack()
    {
        var mesh = TrackModelGenerator.GenerateTracksBetween(
            start.transform.position, control.transform.position, end.transform.position,
            trackTemplate,
            segments
        );

        outputFilter.sharedMesh = mesh;
    }
}
