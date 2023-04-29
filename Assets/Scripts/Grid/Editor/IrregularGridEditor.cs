using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(IrregularGrid))]
public class IrregularGridEditor : Editor
{
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        if (GUILayout.Button("Regenerate"))
        {
            ((IrregularGrid)target).Regenerate();
        }
    }
}
