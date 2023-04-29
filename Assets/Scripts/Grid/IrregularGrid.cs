using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using Sylves;

public class IrregularGrid : MonoBehaviour
{
    [SerializeField]
    private GameObject spawnedModel;

    private void Start()
    {
        var triangleGrid = new TriangleGrid(0.5f, TriangleOrientation.FlatSides, bound: TriangleBound.Hexagon(4));

        var meshData = triangleGrid.ToMeshData();

        // change this to make a pairing that doesn't generate tris
        meshData = meshData.RandomPairing();

        meshData = ConwayOperators.Ortho(meshData);

        meshData = meshData.Weld();

        meshData = meshData.Relax();

        meshData = Matrix4x4.Rotate(Quaternion.Euler(-90f, 0f, 0f)) * meshData;

        Assert.AreEqual(meshData.topologies.Length, 1);
        Assert.AreEqual(meshData.topologies[0], Sylves.MeshTopology.Quads);
        Assert.AreEqual(meshData.indices[0].Length % 4, 0, "Topology was quads but indices array didn't have a multiple of 4 elements");
        for (var meshStartIndex = 0; meshStartIndex < meshData.indices[0].Length; meshStartIndex += 4)
        {
            var vertexIndex0 = meshData.indices[0][meshStartIndex + 0];
            var vertexIndex1 = meshData.indices[0][meshStartIndex + 1];
            var vertexIndex2 = meshData.indices[0][meshStartIndex + 2];
            var vertexIndex3 = meshData.indices[0][meshStartIndex + 3];

            var cageWarper = CageWarper.FromVertices(
                meshData.vertices[vertexIndex0],
                meshData.vertices[vertexIndex1],
                meshData.vertices[vertexIndex2],
                meshData.vertices[vertexIndex3]
            );

            // spawn object
            var spawnedObject = GameObject.Instantiate(spawnedModel);
            // find mesh filter
            var meshFilter = spawnedObject.GetComponent<MeshFilter>();
            // get mesh
            // use .mesh and not .sharedMesh to create a new mesh, so I can modify it
            var mesh = meshFilter.mesh;
            // read mesh
            Assert.IsTrue(mesh.isReadable);
            var localVertices = mesh.vertices;

            // warp mesh
            for (var vertexIndex = 0; vertexIndex < localVertices.Length; ++vertexIndex)
            {
                localVertices[vertexIndex] = cageWarper.WarpVertex(localVertices[vertexIndex]);
            }

            // upload mesh back into mesh filter
            mesh.vertices = localVertices;
        }
    }
}
