using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FrameRateLimiter : MonoBehaviour
{
    [SerializeField]
    private int defaultVSync = 1;

    [SerializeField]
    private int defaultVSyncWebGL = 0;

    [SerializeField]
    private int targetFrameRate = 0;

    [SerializeField]
    private bool debugLog = false;

    private int currentRateIndex = 1;
    private int framesThisSecond = 0;

    private void Start()
    {
        currentRateIndex =
            Application.platform == RuntimePlatform.WebGLPlayer ? defaultVSyncWebGL : defaultVSync;
        ApplyCurrentlySelectedRate();
        PrintFMODStats();

        if (targetFrameRate != 0)
        {
            Application.targetFrameRate = targetFrameRate;
        }
    }

    private void PrintFMODStats()
    {
        uint dspBufferLength;
        int dspBufferCount;
        FMODUnity.RuntimeManager.CoreSystem.getDSPBufferSize(
            out dspBufferLength,
            out dspBufferCount
        );
        Debug.LogFormat(
            "FMOD: DSP buffer length = {0}, DSP buffer count = {1}",
            dspBufferLength,
            dspBufferCount
        );
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
            if (debugLog)
            {
                Debug.LogFormat("Frame rate: {0}", framesThisSecond);
            }
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
