using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CageWarper
{
    // must construct using the "FromVertices" static method
    private CageWarper()
    {}

    public static CageWarper FromVertices(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
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
}
