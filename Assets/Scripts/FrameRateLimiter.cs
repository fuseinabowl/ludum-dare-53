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
    private TextMeshProUGUI debugText;

    [SerializeField]
    private bool debugLog = false;

    [SerializeField]
    private bool debugUi = false;

    private int currentRateIndex = 1;
    private int framesThisSecond = 0;

    private void Start()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            currentRateIndex = defaultVSyncWebGL;
        }
        else
        {
            currentRateIndex = defaultVSync;
        }

        currentRateIndex =
            Application.platform == RuntimePlatform.WebGLPlayer ? defaultVSyncWebGL : defaultVSync;
        ApplyCurrentlySelectedRate();

        if (targetFrameRate != 0)
        {
            Application.targetFrameRate = targetFrameRate;
        }

        if (debugText)
        {
            debugText.gameObject.SetActive(debugUi);
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
            if (debugLog)
            {
                Debug.LogFormat("Frame rate: {0}", framesThisSecond);
            }

            uint dspBufferLength;
            int dspBufferCount;
            FMODUnity.RuntimeManager.CoreSystem.getDSPBufferSize(
                out dspBufferLength,
                out dspBufferCount
            );

            if (debugText && debugUi)
            {
                debugText.text = string.Format(
                    "FPS:{0}/{1} VSync:{2}({3}) FMOD:{4}*{5}",
                    framesThisSecond,
                    targetFrameRate,
                    QualitySettings.vSyncCount,
                    QualitySettings.vSyncCount == 0 ? "off" : "on",
                    dspBufferLength,
                    dspBufferCount
                );
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
