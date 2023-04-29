using UnityEditor;
using UnityEngine;

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

    private IrregularGrid CastTarget => (IrregularGrid)target;
}
