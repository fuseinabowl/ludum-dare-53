using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelLoader))]
public class LevelLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var levelLoader = (LevelLoader)target;
        DrawDefaultInspector();

        bool didChange = levelLoader.levels.Length != levelLoader.levelPaths.Length;
        for (int i = 0; !didChange && i < levelLoader.levels.Length; i++)
        {
            didChange = ScenePath(levelLoader.levels[i]) != levelLoader.levelPaths[i];
        }

        if (didChange)
        {
            serializedObject.Update();
            var levelPaths = serializedObject.FindProperty("levelPaths");
            levelPaths.arraySize = levelLoader.levels.Length;
            for (int i = 0; i < levelLoader.levels.Length; i++)
            {
                levelPaths.GetArrayElementAtIndex(i).stringValue = ScenePath(
                    levelLoader.levels[i]
                );
            }
            serializedObject.ApplyModifiedProperties();
        }
    }

    private string ScenePath(SceneAsset scene)
    {
        return scene == null ? "" : AssetDatabase.GetAssetPath(scene);
    }
}
