using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CageWarper
{
    // must construct using the "FromVertices" static method
    private CageWarper()
    {}

    public static CageWarper FromVertices(
        Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
        // normal generation - neighbouring vertices
        Vector3 v0minusX, Vector3 v1minusX,
        Vector3 v2plusX, Vector3 v3plusX,

        Vector3 v0minusZ, Vector3 v1plusZ,
        Vector3 v2plusZ, Vector3 v3minusZ
    )
    {
        var lowerLeftTriX = v3 - v0;
        var lowerLeftTriZ = v1 - v0;

        var upperRightTriX = v2 - v1;
        var upperRightTriZ = v2 - v3;
        return new CageWarper{
            lowerLeftTriX = lowerLeftTriX,
            lowerLeftTriZ = lowerLeftTriZ,
            lowerLeftBias = v0,

            upperRightTriX = upperRightTriX,
            upperRightTriZ = upperRightTriZ,
            upperRightBias = v2 - upperRightTriX - upperRightTriZ,

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
        if (vertex.x + vertex.z <= 1f)
        {
            return LowerLeftWarp(vertex);
        }
        else
        {
            return UpperRightWarp(vertex);
        }
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

    private Vector3 LowerLeftWarp(Vector3 vertex)
    {
        return lowerLeftTriX * vertex.x + Vector3.up * vertex.y + lowerLeftTriZ * vertex.z + lowerLeftBias;
    }

    private Vector3 UpperRightWarp(Vector3 vertex)
    {
        return upperRightTriX * vertex.x + Vector3.up * vertex.y + upperRightTriZ * vertex.z + upperRightBias;
    }

    private static Vector3 CalculateNormal(Vector3 tangent0, Vector3 tangent1)
    {
        var offset = tangent1 - tangent0;
        var offsetNormal = new Vector3(offset.z, offset.y, -offset.x).normalized;
        return offsetNormal;
    }

    private Vector3 lowerLeftTriX;
    private Vector3 lowerLeftTriZ;
    private Vector3 lowerLeftBias;
    private Vector3 upperRightTriX;
    private Vector3 upperRightTriZ;
    private Vector3 upperRightBias;

    private Vector3 v0xNormal;
    private Vector3 v1xNormal;
    private Vector3 v2xNormal;
    private Vector3 v3xNormal;

    private Vector3 v0zNormal;
    private Vector3 v1zNormal;
    private Vector3 v2zNormal;
    private Vector3 v3zNormal;
}
