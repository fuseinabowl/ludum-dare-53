using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using Sylves;

[ExecuteAlways]
public class IrregularGrid : MonoBehaviour
{
    [SerializeField]
    private GameObject spawnedModel;

    [SerializeField]
    private int seed;

    [SerializeField]
    private int mapSize = 4;

    private void Start()
    {
        Generate();
    }

    public void Regenerate()
    {
        ClearChildren();
        Generate();
    }

    private void ClearChildren()
    {
        while (transform.childCount > 0)
        {
            var childTransform = transform.GetChild(0);
            GameObject.DestroyImmediate(childTransform.gameObject);
        }
    }

    private void Generate()
    {
        var triangleGrid = new TriangleGrid(0.5f, TriangleOrientation.FlatSides, bound: TriangleBound.Hexagon(mapSize));

        var meshData = triangleGrid.ToMeshData();

        var seededRng = new System.Random(seed);

        // change this to make a pairing that doesn't generate tris
        meshData = meshData.RandomPairing(() => seededRng.NextDouble());

        meshData = ConwayOperators.Ortho(meshData);

        meshData = meshData.Weld();

        meshData = meshData.Relax();

        meshData = Matrix4x4.Rotate(Quaternion.Euler(-90f, 0f, 0f)) * meshData;

        VertexNetwork net;

        if (TryGetComponent<VertexNetwork>(out net)) {
            net.SetEdgeGraph(CreateEdgeGraph(meshData.vertices, meshData.indices[0]));
        }

        Assert.AreEqual(meshData.topologies.Length, 1);
        Assert.AreEqual(meshData.topologies[0], Sylves.MeshTopology.Quads);
        Assert.AreEqual(meshData.indices[0].Length % 4, 0, "Topology was quads but indices array didn't have a multiple of 4 elements");

        var indices = meshData.indices[0];
        var vertices = meshData.vertices;

        for (var meshStartIndex = 0; meshStartIndex < indices.Length; meshStartIndex += 4)
        {
            var vertexIndex0 = indices[meshStartIndex + 0];
            var vertexIndex1 = indices[meshStartIndex + 1];
            var vertexIndex2 = indices[meshStartIndex + 2];
            var vertexIndex3 = indices[meshStartIndex + 3];

            var quadNeighbour01 = FindQuadNeighbour(indices, vertexIndex0, vertexIndex1, meshStartIndex);
            var quadNeighbour12 = FindQuadNeighbour(indices, vertexIndex1, vertexIndex2, meshStartIndex);
            var quadNeighbour23 = FindQuadNeighbour(indices, vertexIndex2, vertexIndex3, meshStartIndex);
            var quadNeighbour30 = FindQuadNeighbour(indices, vertexIndex3, vertexIndex0, meshStartIndex);

            var cageWarper = CageWarper.FromVertices(
                meshData.vertices[vertexIndex0],
                meshData.vertices[vertexIndex1],
                meshData.vertices[vertexIndex2],
                meshData.vertices[vertexIndex3],

                FindTangentVertex(indices, vertices, vertexIndex0, vertexIndex1, quadNeighbour01),
                FindTangentVertex(indices, vertices, vertexIndex1, vertexIndex0, quadNeighbour01),
                FindTangentVertex(indices, vertices, vertexIndex2, vertexIndex3, quadNeighbour23),
                FindTangentVertex(indices, vertices, vertexIndex3, vertexIndex2, quadNeighbour23),

                FindTangentVertex(indices, vertices, vertexIndex0, vertexIndex3, quadNeighbour30),
                FindTangentVertex(indices, vertices, vertexIndex1, vertexIndex2, quadNeighbour12),
                FindTangentVertex(indices, vertices, vertexIndex2, vertexIndex1, quadNeighbour12),
                FindTangentVertex(indices, vertices, vertexIndex3, vertexIndex0, quadNeighbour30)
            );

            // spawn object
            var spawnedObject = GameObject.Instantiate(spawnedModel, transform);
            spawnedObject.hideFlags = HideFlags.DontSave;
            // find mesh filter
            var meshFilter = spawnedObject.GetComponent<MeshFilter>();
            // get mesh
            var originalMesh = meshFilter.sharedMesh;
            var mesh = Mesh.Instantiate(originalMesh);
            meshFilter.sharedMesh = mesh;
            // read mesh
            Assert.IsTrue(mesh.isReadable);
            var localVertices = mesh.vertices;
            var localNormals = mesh.normals;

            // warp mesh
            for (var vertexIndex = 0; vertexIndex < localVertices.Length; ++vertexIndex)
            {
                localNormals[vertexIndex] = cageWarper.WarpNormal(localVertices[vertexIndex], localNormals[vertexIndex]);
                localVertices[vertexIndex] = cageWarper.WarpVertex(localVertices[vertexIndex]);
            }

            // upload mesh back into mesh filter
            mesh.vertices = localVertices;
            mesh.normals = localNormals;

            mesh.RecalculateBounds();
        }
    }

    private int FindQuadNeighbour(int[] indices, int index0, int index1, int currentQuadStartIndex)
    {
        for (var quadStartIndex = 0; quadStartIndex < indices.Length; quadStartIndex += 4)
        {
            if (currentQuadStartIndex == quadStartIndex)
            {
                continue;
            }

            for (var cornerIndex = 0; cornerIndex < 4; ++cornerIndex)
            {
                var investigatingIndex = quadStartIndex + cornerIndex;
                if (indices[investigatingIndex] == index0 || indices[investigatingIndex] == index1)
                {
                    var nextCornerIndex = (cornerIndex + 1) % 4;
                    var adjacentInvestigatingIndex = quadStartIndex + nextCornerIndex;
                    if (indices[adjacentInvestigatingIndex] == index0 || indices[adjacentInvestigatingIndex] == index1)
                    {
                        return quadStartIndex;
                    }
                }
            }
        }

        // couldn't find an adjacent quad in this direction
        // maybe this quad is on the edge of the grid?
        return -1;
    }

    // normally finds the referenced quad's adjacent vertex that isn't "adjacentVertexOnThisQuad"
    // if the adjacentQuadStartIndex is -1 (missing) then returns thisVertex as a best guess
    private Vector3 FindTangentVertex(int[] indices, Vector3[] vertices, int thisVertexIndex, int adjacentVertexOnThisQuadIndex, int adjacentQuadStartIndex)
    {
        if (adjacentQuadStartIndex == -1)
        {
            return vertices[indices[thisVertexIndex]];
        }

        Assert.AreEqual(adjacentQuadStartIndex % 4, 0);

        var referencedCornerIndex = FindIndexInQuad(indices, adjacentQuadStartIndex, thisVertexIndex);
        var nextIndex = adjacentQuadStartIndex + ((referencedCornerIndex + 1) % 4);
        // use +4-1 to avoid negative numbers in the modulo, which causes unintuitive behaviour
        var previousIndex = adjacentQuadStartIndex + ((referencedCornerIndex + 4 - 1) % 4);

        if (indices[nextIndex] == adjacentVertexOnThisQuadIndex)
        {
            Assert.AreNotEqual(indices[previousIndex], adjacentVertexOnThisQuadIndex);
            return vertices[indices[previousIndex]];
        }
        else
        {
            Assert.AreEqual(indices[previousIndex], adjacentVertexOnThisQuadIndex, $"Next index {nextIndex} was not equal to {adjacentVertexOnThisQuadIndex}, so the previous index {previousIndex} should have been");
            return vertices[indices[nextIndex]];
        }
    }

    private int FindIndexInQuad(int[] indices, int quadStartIndex, int thisVertexIndex)
    {
        for (var cornerIndex = 0; cornerIndex < 4; ++cornerIndex)
        {
            var investigatingIndex = quadStartIndex + cornerIndex;
            if (indices[investigatingIndex] == thisVertexIndex)
            {
                return investigatingIndex;
            }
        }

        Assert.IsTrue(false, $"Couldn't find this vertex index ({thisVertexIndex}) in this quad ({quadStartIndex / 4})");
        return -1;
    }

    private static EdgeGraph CreateEdgeGraph(Vector3[] badVertices, int[] quadEdges) {
        List<Vector3> vertices = new List<Vector3>(badVertices.Length);
        foreach (var vertex in badVertices) {
            vertices.Add(Vector3.Scale(vertex, new Vector3(-1, 1, -1)));
        }

        List<Edge> edges = new List<Edge>();
        for (int i = 0; i < quadEdges.Length; i += 4) {
            for (int j = 0; j < 4; j++) {
                edges.Add(new Edge(
                    vertices[quadEdges[i + j]],
                    vertices[quadEdges[i + (j + 1) % 4]]
                ));
            }
        }
        
        return new EdgeGraph(vertices, edges);
    }
}
