using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnityEngine.Canvas))]
public class PauseMenu : MonoBehaviour
{
    public bool hideOnStart = true;

    public bool pauseOnShow = true;

    public bool toggleOnEsc = true;

    private Canvas canvas;

    public void Show()
    {
        canvas.enabled = true;
        if (pauseOnShow)
        {
            Time.timeScale = 0;
        }
    }

    public void Hide()
    {
        canvas.enabled = false;
        if (pauseOnShow)
        {
            Time.timeScale = 1;
        }
    }

    public void Toggle()
    {
        if (canvas.enabled)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
    }

    private void Start()
    {
        if (hideOnStart)
        {
            Hide();
        }
    }

    private void Update()
    {
        if (toggleOnEsc && Input.GetKeyDown(KeyCode.Escape))
        {
            Toggle();
        }
    }
}
