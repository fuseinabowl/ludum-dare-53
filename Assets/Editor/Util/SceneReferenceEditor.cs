using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SceneReference))]
public class SceneReferenceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var sceneRef = (SceneReference)target;
        DrawDefaultInspector();

        string scenePath = "";

        if (sceneRef.sceneAsset != null)
        {
            scenePath = AssetDatabase.GetAssetPath(sceneRef.sceneAsset);
        }

        if (scenePath != sceneRef.scenePath)
        {
            serializedObject.Update();
            var scenePathProp = serializedObject.FindProperty("scenePath");
            scenePathProp.stringValue = scenePath;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
