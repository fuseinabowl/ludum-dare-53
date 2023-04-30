using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackModelGeneratorComponent : MonoBehaviour
{
    [SerializeField]
    private Mesh templateMesh;
    [SerializeField]
    private MeshFilter outputFilter;
    [SerializeField]
    [Min(1)]
    private int segments = 4;

    private Vector3 cachedStart;
    private Vector3 cachedControl;
    private Vector3 cachedEnd;

    public void SetPoints(Vector3 start, Vector3 control, Vector3 end)
    {
        cachedStart = start;
        cachedControl = control;
        cachedEnd = end;

        GenerateAndApplyMeshFromCache();
    }

    private void GenerateAndApplyMeshFromCache()
    {
        var mesh = TrackModelGenerator.GenerateTracksBetween(cachedStart, cachedControl, cachedEnd, templateMesh, segments);
        outputFilter.sharedMesh = mesh;
    }

    public void SetEnd(Vector3 end)
    {
        cachedEnd = end;

        GenerateAndApplyMeshFromCache();
    }
}
