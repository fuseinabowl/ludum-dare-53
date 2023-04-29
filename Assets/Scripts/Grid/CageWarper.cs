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
        Vector3 v2plusZ, Vector3 v3minusZ,
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

            v0xNormal = CalculateNormal(v0minusX, v3),
            v1xNormal = CalculateNormal(v1minusX, v2),
            v2xNormal = CalculateNormal(v1, v2plusX),
            v3xNormal = CalculateNormal(v0, v3plusX),

            v0zNormal = CalculateNormal(v0minusZ, v1),
            v1zNormal = CalculateNormal(v0, v1plusZ),
            v2zNormal = CalculateNormal(v3, v2plusZ),
            v3zNormal = CalculateNormal(v3minusZ, v2),
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
        return normal;
    }

    private Vector3 LowerLeftWarp(Vector3 vertex)
    {
        return lowerLeftTriX * vertex.x + Vector3.up * vertex.y + lowerLeftTriZ * vertex.z + lowerLeftBias;
    }

    private Vector3 UpperRightWarp(Vector3 vertex)
    {
        return upperRightTriX * vertex.x + Vector3.up * vertex.y + upperRightTriZ * vertex.z + upperRightBias;
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
