using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class SceneReference : MonoBehaviour
{
#if UNITY_EDITOR
    public UnityEditor.SceneAsset sceneAsset;
#endif

    [HideInInspector]
    public string scenePath;

    public void Load()
    {
        if (scenePath == "")
        {
            Debug.LogWarning("SceneReference does not have a path");
            return;
        }

#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode(
            scenePath,
            new LoadSceneParameters(LoadSceneMode.Single)
        );
#else
        SceneManager.LoadScene(scenePath);
#endif
    }
}
