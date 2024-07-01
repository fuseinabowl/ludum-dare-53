using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadScene : MonoBehaviour
{
    void Awake()
    {
        var loader = GetComponent<FMODLoader>();

        if (loader.loaded)
        {
            Debug.Log("bank already loaded, going to next scene");
            LoadSceneReference();
        }
        else
        {
            Debug.Log("bank not loaded yet, waiting...");
            loader.onLoaded += FMODDidLoad;
        }
    }

    void FMODDidLoad(FMODLoader loader)
    {
        LoadSceneReference();
    }

    void LoadSceneReference()
    {
        if (GetComponent<SceneReference>().scenePath != "")
        {
            GetComponent<SceneReference>().Load();
        }
    }
}
