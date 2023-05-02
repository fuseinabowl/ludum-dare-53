using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FrameRateLimiter : MonoBehaviour
{
    private int currentRateIndex = 2;

    private void Start()
    {
        ApplyCurrentlySelectedRate();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            IncrementAndApplyFrameRate();
        }
    }

    private void ApplyCurrentlySelectedRate()
    {
        Debug.Log($"Applying VSync of {currentRateIndex}");
        QualitySettings.vSyncCount = currentRateIndex;
    }

    public void IncrementAndApplyFrameRate()
    {
        ++currentRateIndex;
        if (currentRateIndex > 4) // hard coded limit in Unity: https://docs.unity3d.com/ScriptReference/QualitySettings-vSyncCount.html
        {
            currentRateIndex = 0;
        }

        ApplyCurrentlySelectedRate();
    }
}
