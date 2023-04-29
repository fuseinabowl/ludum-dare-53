using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using Sylves;

public class IrregularGrid : MonoBehaviour
{
    [SerializeField]
    private MeshFilter meshFilter;

    private void Start()
    {
        var triangleGrid = new TriangleGrid(0.5f, TriangleOrientation.FlatSides, bound: TriangleBound.Hexagon(4));

        var meshData = triangleGrid.ToMeshData();

        // change this to make a pairing that doesn't generate tris
        meshData = meshData.RandomPairing();

        meshData = ConwayOperators.Ortho(meshData);

        meshData = meshData.Weld();

        meshData = meshData.Relax();

        var mesh = meshData.ToMesh();
        Assert.IsNotNull(meshFilter);
        meshFilter.sharedMesh = mesh;
    }
}
