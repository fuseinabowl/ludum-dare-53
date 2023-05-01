using UnityEditor;
using UnityEngine;
using Sylves;

[CustomEditor(typeof(IrregularGrid))]
public class IrregularGridEditor : Editor
{
    public override void OnInspectorGUI() {
        var selectedPaletteObject = FindSelectedPaletteObject(useExcept: false);
        using (var check = new EditorGUI.ChangeCheckScope())
        {
            DrawDefaultInspector();

            if (check.changed)
            {
                var afterChangeSelectedPaletteObject = FindSelectedPaletteObject(useExcept: true, except: selectedPaletteObject);
                if (afterChangeSelectedPaletteObject != null && afterChangeSelectedPaletteObject != selectedPaletteObject)
                {
                    DeselectAllPaletteObjectsExcept(afterChangeSelectedPaletteObject);
                }
            }
        }

        if (GUILayout.Button("Regenerate"))
        {
            ((IrregularGrid)target).Regenerate();
        }

        if (GUILayout.Button("Bake grid"))
        {
            BakeGrid();
            ((IrregularGrid)target).Regenerate();
        }
    }

    private GameObject FindSelectedPaletteObject(bool useExcept, GameObject except = null)
    {
        var palette = serializedObject.FindProperty(nameof(IrregularGrid.mapEditorPalette));
        for (var optionIndex = 0; optionIndex < palette.arraySize; ++optionIndex)
        {
            var option = palette.GetArrayElementAtIndex(optionIndex);
            var prefab = (GameObject)option.FindPropertyRelative(nameof(IrregularGrid.PaletteOption.prefab)).objectReferenceValue;
            if (!useExcept || prefab != except)
            {
                var isSelected = option.FindPropertyRelative(nameof(IrregularGrid.PaletteOption.isSelected)).boolValue;
                if (isSelected)
                {
                    return prefab;
                }
            }
        }

        return null;
    }

    private void DeselectAllPaletteObjectsExcept(GameObject except)
    {
        var palette = serializedObject.FindProperty(nameof(IrregularGrid.mapEditorPalette));
        for (var optionIndex = 0; optionIndex < palette.arraySize; ++optionIndex)
        {
            var option = palette.GetArrayElementAtIndex(optionIndex);
            var prefab = (GameObject)option.FindPropertyRelative(nameof(IrregularGrid.PaletteOption.prefab)).objectReferenceValue;
            if (prefab != except)
            {
                option.FindPropertyRelative(nameof(IrregularGrid.PaletteOption.isSelected)).boolValue = false;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void BakeGrid()
    {
        var mesh = GenerateMesh();
        SaveMeshToAssets(mesh);
        AssignMeshToIrregularGrid(mesh);
    }

    private void AssignMeshToIrregularGrid(Mesh mesh)
    {
        Undo.RecordObject(target, "Baking irregular grid");
        var bakedMeshProperty = serializedObject.FindProperty(nameof(IrregularGrid.bakedGridMesh));
        bakedMeshProperty.objectReferenceValue = mesh;
        serializedObject.ApplyModifiedProperties();
    }

    private void SaveMeshToAssets(Mesh mesh)
    {
        AssetDatabase.CreateAsset(mesh, "Assets/Baked Maps/map.mesh");
    }

    private Mesh GenerateMesh()
    {
        var meshData = GenerateMeshData();
        var mesh = meshData.ToMesh();
        return mesh;
    }

    private MeshData GenerateMeshData()
    {
        var triangleGrid = new TriangleGrid(CastTarget.GridData.cellSide, TriangleOrientation.FlatSides, bound: TriangleBound.Hexagon(CastTarget.GridData.mapSize));

        var meshData = triangleGrid.ToMeshData();

        var rng = new LocalRng.Random(CastTarget.GridData.seed);

        // change this to make a pairing that doesn't generate tris
        meshData = meshData.RandomPairing(() => rng.NextDouble());

        meshData = ConwayOperators.Ortho(meshData);

        meshData = meshData.Weld(tolerance:1e-1f);

        TownscaperRelaxer.Relax(meshData, CastTarget.GridData.relaxSize, CastTarget.RelaxIterations, CastTarget.RelaxStrength);

        return Matrix4x4.Rotate(Quaternion.Euler(-90f, 0f, 0f)) * meshData;
    }

    private IrregularGrid CastTarget => (IrregularGrid)target;
}
