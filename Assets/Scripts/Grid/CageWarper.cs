using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CageWarper
{
    // must construct using the "FromVertices" static method
    private CageWarper()
    {}

    public static CageWarper FromVerticesWithTrackNormals(
        Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
        Vector3 v03xNormal, Vector3 v12xNormal,
        Vector3 v01yNormal, Vector3 v23yNormal
    )
    {
        return new CageWarper{
            v0 = v0,
            v1 = v1,
            v2 = v2,
            v3 = v3,

            v0xNormal = v03xNormal,
            v1xNormal = v12xNormal,
            v2xNormal = v12xNormal,
            v3xNormal = v03xNormal,

            v0zNormal = v01yNormal,
            v1zNormal = v01yNormal,
            v2zNormal = v23yNormal,
            v3zNormal = v23yNormal,
        };
    }

    public static CageWarper FromVertices(
        Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
        // normal generation - neighbouring vertices
        Vector3 v0minusX, Vector3 v1minusX,
        Vector3 v2plusX, Vector3 v3plusX,

        Vector3 v0minusZ, Vector3 v1plusZ,
        Vector3 v2plusZ, Vector3 v3minusZ
    )
    {
        return new CageWarper{
            v0 = v0,
            v1 = v1,
            v2 = v2,
            v3 = v3,

            v0xNormal = CalculateNormal(v0minusZ, v1),
            v1xNormal = CalculateNormal(v0, v1plusZ),
            v2xNormal = CalculateNormal(v3, v2plusZ),
            v3xNormal = CalculateNormal(v3minusZ, v2),

            v0zNormal = -CalculateNormal(v0minusX, v3),
            v1zNormal = -CalculateNormal(v1minusX, v2),
            v2zNormal = -CalculateNormal(v1, v2plusX),
            v3zNormal = -CalculateNormal(v0, v3plusX),
        };
    }

    public Vector3 WarpVertex(Vector3 vertex)
    {
        return BilinearBlend(vertex, v0, v1, v2, v3) + Vector3.up * vertex.y;
    }

    public Vector3 WarpNormal(Vector3 vertex, Vector3 normal)
    {
        var normalX = BilinearBlend(vertex, v0xNormal, v1xNormal, v2xNormal, v3xNormal);
        var normalZ = BilinearBlend(vertex, v0zNormal, v1zNormal, v2zNormal, v3zNormal);

        return normalX * normal.x + Vector3.up * normal.y + normalZ * normal.z;
    }

    private Vector3 BilinearBlend(Vector3 vertex, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        var leftValue = Vector3.Lerp(v0, v1, vertex.z);
        var rightValue = Vector3.Lerp(v3, v2, vertex.z);
        return Vector3.Lerp(leftValue, rightValue, vertex.x);
    }

    private static Vector3 CalculateNormal(Vector3 tangent0, Vector3 tangent1)
    {
        var offset = tangent1 - tangent0;
        var offsetNormal = new Vector3(offset.z, offset.y, -offset.x).normalized;
        return offsetNormal;
    }

    private Vector3 v0;
    private Vector3 v1;
    private Vector3 v2;
    private Vector3 v3;

    private Vector3 v0xNormal;
    private Vector3 v1xNormal;
    private Vector3 v2xNormal;
    private Vector3 v3xNormal;

    private Vector3 v0zNormal;
    private Vector3 v1zNormal;
    private Vector3 v2zNormal;
    private Vector3 v3zNormal;
}
