using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FreeFollowCamera))]
public class FreeFollowCameraEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        using (var changeCheck = new EditorGUI.ChangeCheckScope())
        {
            DrawBoolPrefInspector(CameraPlayerPrefs.flipCameraRotation, "Flip rotation");
            DrawBoolPrefInspector(CameraPlayerPrefs.flipCameraZoom, "Flip zoom");

            if (changeCheck.changed)
            {
                PlayerPrefs.Save();
            }
        }
    }

    private void DrawBoolPrefInspector(string key, string name)
    {
        var currentFlipRotation = PlayerPrefs.GetInt(key, 0) != 0;
        var newFlipRotation = EditorGUILayout.Toggle(name, currentFlipRotation);
        PlayerPrefs.SetInt(key, newFlipRotation ? 1 : 0);
    }
}
