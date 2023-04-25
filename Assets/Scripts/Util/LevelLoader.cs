using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Sequentially loads scenes. Has a convenient interface where in editor mode the scene assets
/// can be dragged into the list, but in play mode uses the paths of those scenes.
/// See LevelLoaderEditor for how this works.
/// </summary>
public class LevelLoader : MonoBehaviour
{
#if UNITY_EDITOR
    public UnityEditor.SceneAsset[] levels = new UnityEditor.SceneAsset[] { };
#endif

    [HideInInspector]
    public string[] levelPaths = new string[] { };

    /// <summary>
    /// Loads the next level Scene and returns true (for what it's worth). If there is no
    /// next level then returns false and doesn't do anything.
    /// </summary>
    public bool LoadNextLevel()
    {
        var currentScene = SceneManager.GetActiveScene();
        int nextScene = 0;

        for (int i = 0; i < levelPaths.Length; i++)
        {
            if (levelPaths[i] == currentScene.path)
            {
                nextScene = i + 1;
            }
        }

        if (nextScene > 0 && nextScene < levelPaths.Length)
        {
            var path = levelPaths[nextScene];
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(
                path,
                new LoadSceneParameters(LoadSceneMode.Single)
            );
#else
            SceneManager.LoadScene(path);
#endif
            return true;
        }

        return false;
    }
}
