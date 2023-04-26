using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class NextLevelButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        LevelLoader loader;
        if (SingletonProvider.TryGet(out loader))
        {
            loader.LoadNextLevel();
        }
        else
        {
            Debug.LogError("Tried to load next level but couldn't find a LevelLoader, " +
                "is there one in this scene with a SingletonProvider?");
        }
    }
}
