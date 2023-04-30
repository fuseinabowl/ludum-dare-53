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

    public void SetPoints(Vector3 start, Vector3 control)
    {
        cachedStart = start;
        cachedControl = control;
        cachedEnd = CalculateIdleEndPoint(start, control);

        GenerateAndApplyMeshFromCache();
    }

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

    public void SetEndToIdle()
    {
        cachedEnd = CalculateIdleEndPoint(cachedStart, cachedControl);

        GenerateAndApplyMeshFromCache();
    }

    private Vector3 CalculateIdleEndPoint(Vector3 start, Vector3 control)
    {
        return CalculateOverExtendedVertex(start, control, overextendDistance:0.1f);
    }

    private static Vector3 CalculateOverExtendedVertex(Vector3 start, Vector3 end, float overextendDistance)
    {
        var offset = end - start;
        return end + offset.normalized * overextendDistance;
    }
}
