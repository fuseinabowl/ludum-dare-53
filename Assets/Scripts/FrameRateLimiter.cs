using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FrameRateLimiter : MonoBehaviour
{
    [SerializeField]
    private int defaultVSync = 0;

    [SerializeField]
    private int targetFrameRate = 0;

    private int currentRateIndex = 2;
    private int framesThisSecond = 0;

    private void Start()
    {
        currentRateIndex = defaultVSync;
        ApplyCurrentlySelectedRate();

        if (targetFrameRate != 0)
        {
            Application.targetFrameRate = targetFrameRate;
        }
    }

    private void Update()
    {
        framesThisSecond++;
        if (Input.GetKeyDown(KeyCode.P))
        {
            IncrementAndApplyFrameRate();
        }
    }

    private void OnTimerTick(Timer timer)
    {
        if (timer.timerName == "FrameRateTimer")
        {
            Debug.LogFormat("Frame rate: {0}", framesThisSecond);
            framesThisSecond = 0;
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
