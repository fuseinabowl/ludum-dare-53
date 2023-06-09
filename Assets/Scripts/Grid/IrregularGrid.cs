using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

using Sylves;

[ExecuteAlways]
[SelectionBase]
public class IrregularGrid : MonoBehaviour
{
    [SerializeField]
    private GridData gridData;
    public GridData GridData => gridData;

    [Serializable]
    public class PaletteOption
    {
        public bool isSelected = false;
        public GameObject prefab;
    }
    [SerializeField]
    public List<PaletteOption> mapEditorPalette = new List<PaletteOption>();
    public GameObject CurrentPaletteOption => mapEditorPalette.First(option => option.isSelected)?.prefab;

    private VertexNetwork vertexNetwork;

    [SerializeField]
    [HideInInspector]
    public Mesh bakedGridMesh;

    private MeshData meshData;

    [SerializeField]
    [Min(0)]
    private int relaxIterations = 10;
    public int RelaxIterations => relaxIterations;
    [SerializeField]
    private float relaxStrength = 1e-3f;
    public float RelaxStrength => relaxStrength;

    [SerializeField]
    private Material[] combinedMaterials = new Material[0];

    private void Awake() {
        vertexNetwork = GetComponent<VertexNetwork>();
    }

    private void Start()
    {

        if (Application.IsPlaying(gameObject))
        {
            GenerateAndCombineMeshes();
        }
        else
        {
            Generate();
        }
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
        meshData = LoadMeshData();

        if (vertexNetwork) {
            vertexNetwork.SetEdgeGraph(CreateEdgeGraph(meshData.vertices, meshData.indices[0]));
        }

        Assert.AreEqual(meshData.topologies.Length, 1);
        Assert.AreEqual(meshData.topologies[0], Sylves.MeshTopology.Quads);
        Assert.AreEqual(meshData.indices[0].Length % 4, 0, "Topology was quads but indices array didn't have a multiple of 4 elements");

        var indices = meshData.indices[0];

        for (var quadIndex = 0; quadIndex < indices.Length / 4; ++quadIndex)
        {
            CreatePrefabInSlot(quadIndex);
        }
    }

    private Sylves.MeshData LoadMeshData()
    {
        Assert.IsNotNull(bakedGridMesh);
        return new MeshData(bakedGridMesh);
    }

    private const int vertexDangerLimit = 60000;
    private void GenerateAndCombineMeshes()
    {
        meshData = LoadMeshData();

        if (vertexNetwork) {
            vertexNetwork.SetEdgeGraph(CreateEdgeGraph(meshData.vertices, meshData.indices[0]));
        }

        Assert.AreEqual(meshData.topologies.Length, 1);
        Assert.AreEqual(meshData.topologies[0], Sylves.MeshTopology.Quads);
        Assert.AreEqual(meshData.indices[0].Length % 4, 0, "Topology was quads but indices array didn't have a multiple of 4 elements");

        var indices = meshData.indices[0];

        var accumulatingVertices = new List<Vector3>();
        var accumulatingNormals = new List<Vector3>();
        var accumulatingUv0s = new List<Vector2>();
        var accumulatingUv1s = new List<Vector2>();
        var accumulatingIndices = new List<int>();

        for (var quadIndex = 0; quadIndex < indices.Length / 4; ++quadIndex)
        {
            AppendWarpedSlotPrefabToMeshDataAndSpawnProps(quadIndex, accumulatingVertices, accumulatingNormals, accumulatingUv0s, accumulatingUv1s, accumulatingIndices);

            if (accumulatingVertices.Count > vertexDangerLimit)
            {
                EmitMeshRenderer(
                    accumulatingVertices,
                    accumulatingNormals,
                    accumulatingUv0s,
                    accumulatingUv1s,
                    accumulatingIndices
                );
                accumulatingVertices.Clear();
                accumulatingNormals.Clear();
                accumulatingUv0s.Clear();
                accumulatingUv1s.Clear();
                accumulatingIndices.Clear();
            }
        }

        EmitMeshRenderer(
            accumulatingVertices,
            accumulatingNormals,
            accumulatingUv0s,
            accumulatingUv1s,
            accumulatingIndices
        );
    }

    private void EmitMeshRenderer(
        List<Vector3> accumulatingVertices,
        List<Vector3> accumulatingNormals,
        List<Vector2> accumulatingUv0s,
        List<Vector2> accumulatingUv1s,
        List<int> accumulatingIndices
    )
    {
        var mesh = new Mesh();

        mesh.vertices = accumulatingVertices.ToArray();
        mesh.normals = accumulatingNormals.ToArray();
        mesh.SetUVs(0, accumulatingUv0s.ToArray());
        mesh.SetUVs(1, accumulatingUv1s.ToArray());
        mesh.SetIndices(accumulatingIndices.ToArray(), UnityEngine.MeshTopology.Triangles, 0);

        mesh.RecalculateBounds();

        var meshHolderObject = new GameObject();
        meshHolderObject.transform.parent = transform;

        var meshFilter = meshHolderObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        var meshRenderer = meshHolderObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = combinedMaterials;
    }

    private void AppendWarpedSlotPrefabToMeshDataAndSpawnProps(int quadIndex, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv0, List<Vector2> uv1, List<int> indices)
    {
        var warpedData = InstantiateAndWarpTilePrefab(quadIndex);

        var baseVertex = vertices.Count;
        vertices.AddRange(warpedData.vertices);
        normals.AddRange(warpedData.normals);
        uv0.AddRange(warpedData.uv0);
        uv1.AddRange(warpedData.uv1);
        indices.AddRange(warpedData.subMesh0Indices.Select(index => index + baseVertex));

        UnityEngine.Object.Destroy(warpedData.tileInstance.GetComponent<MeshFilter>());
        UnityEngine.Object.Destroy(warpedData.tileInstance.GetComponent<MeshRenderer>());

        var rotationMatrix = GetTileTransform(quadIndex);
        SpawnPropsOnObject(warpedData.tileInstance, rotationMatrix, warpedData.warper);
    }

    private GridData.QuadOverride GetOverrideForQuad(int quadIndex)
    {
        if (quadIndex < gridData.matchingOrderPrefabOverrides.Count)
        {
            return gridData.matchingOrderPrefabOverrides[quadIndex];
        }

        return null;
    }

    public void CreatePrefabInSlot(int quadIndex)
    {
        // on recompile, mesh data is lost
        // recreate it here if it doesn't exist
        EnsureMeshDataValid();

        var warpedData = InstantiateAndWarpTilePrefab(quadIndex);
        var meshFilter = warpedData.tileInstance.GetComponent<MeshFilter>();
        var mesh = meshFilter.sharedMesh;
        mesh.vertices = warpedData.vertices;
        mesh.normals = warpedData.normals;

        mesh.RecalculateBounds();

        var quadIndexRegister = warpedData.tileInstance.AddComponent<GridQuadIndexRegister>();
        quadIndexRegister.quadIndex = quadIndex;

        var rotationMatrix = GetTileTransform(quadIndex);
        SpawnPropsOnObject(warpedData.tileInstance, rotationMatrix, warpedData.warper);
    }

    private class WarpedMeshData
    {
        public GameObject tileInstance;
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector2[] uv0;
        public Vector2[] uv1;
        public int[] subMesh0Indices;
        public CageWarper warper;
    }

    private Matrix4x4 GetTileTransform(int quadIndex)
    {
        var quadOverride = GetOverrideForQuad(quadIndex);
        var rotationIndex = quadOverride?.rotationIndex ?? 0;
        return Matrix4x4.Translate(new Vector3(0.5f, 0f, 0.5f))
             * Matrix4x4.Rotate(Quaternion.AngleAxis(90f * rotationIndex, Vector3.up))
             * Matrix4x4.Translate(new Vector3(-0.5f, 0f, -0.5f));
    }

    private WarpedMeshData InstantiateAndWarpTilePrefab(int quadIndex)
    {
        var quadOverride = GetOverrideForQuad(quadIndex);
        var selectedPrefab = quadOverride?.prefab != null ? quadOverride.prefab : gridData.defaultSpawnedModel;

        var rotationMatrix = GetTileTransform(quadIndex);

        // on recompile, mesh data is lost
        // recreate it here if it doesn't exist
        EnsureMeshDataValid();

        var indices = meshData.indices[0];
        var vertices = meshData.vertices;

        var meshStartIndex = quadIndex * 4;

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
        var spawnedObject = GameObject.Instantiate(selectedPrefab, transform);
        spawnedObject.hideFlags = HideFlags.DontSave;
        // find mesh filter
        var meshFilter = spawnedObject.GetComponent<MeshFilter>();
        // get mesh
        var originalMesh = meshFilter.sharedMesh;
        var mesh = Mesh.Instantiate(originalMesh);
        meshFilter.sharedMesh = mesh;
        // read mesh
        Assert.IsTrue(mesh.isReadable);
        var localVertices = mesh.vertices.Select(vertex => rotationMatrix.MultiplyPoint(vertex)).ToArray();
        var localNormals = mesh.normals.Select(vertex => rotationMatrix.MultiplyVector(vertex)).ToArray();

        // warp mesh
        for (var vertexIndex = 0; vertexIndex < localVertices.Length; ++vertexIndex)
        {
            localNormals[vertexIndex] = cageWarper.WarpNormal(localVertices[vertexIndex], localNormals[vertexIndex]);
            localVertices[vertexIndex] = cageWarper.WarpVertex(localVertices[vertexIndex]);
        }

        var uv0 = new List<Vector2>();
        mesh.GetUVs(0, uv0);
        var uv1 = new List<Vector2>();
        mesh.GetUVs(1, uv1);
        return new WarpedMeshData{
            tileInstance = spawnedObject,
            vertices = localVertices,
            normals = localNormals,
            uv0 = uv0.ToArray(),
            uv1 = uv1.ToArray(),
            subMesh0Indices = mesh.GetIndices(0),
            warper = cageWarper,
        };
    }

    private void SpawnPropsOnObject(GameObject instance, Matrix4x4 preWarpTransform, CageWarper warper)
    {
        foreach (var propSpawner in instance.GetComponentsInChildren<PropSpawner>())
        {
            propSpawner.Spawn(preWarpTransform, warper);
        }
    }

    private void EnsureMeshDataValid()
    {
        if (meshData == null)
        {
            meshData = LoadMeshData();
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

    private static EdgeGraph CreateEdgeGraph(Vector3[] vertices, int[] quadEdges) {
        List<Edge> edges = new List<Edge>();
        for (int i = 0; i < quadEdges.Length; i += 4) {
            for (int j = 0; j < 4; j++) {
                edges.Add(new Edge(
                    vertices[quadEdges[i + j]],
                    vertices[quadEdges[i + (j + 1) % 4]]
                ));
            }
        }
        
        return new EdgeGraph(edges);
    }
}
