using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FrameRateLimiter : MonoBehaviour
{
    [Serializable]
    public class FrameRateOption
    {
        public int targetFrameRate = -1;
        public string displayName = "infinite";
    }

    [SerializeField]
    private List<FrameRateOption> frameRateOptions = new List<FrameRateOption>();

    [SerializeField]
    private TextMeshProUGUI hudText;
    
    [SerializeField]
    [Tooltip("{0} is the FPS target name")]
    [Multiline]
    private string formatString;

    private int currentRateIndex = 0;

    private void Start()
    {
        ApplyCurrentlySelectedRate();
    }

    private void ApplyCurrentlySelectedRate()
    {
        if (frameRateOptions.Count > currentRateIndex && currentRateIndex >= 0)
        {
            var optionData = frameRateOptions[currentRateIndex];
            Application.targetFrameRate = optionData.targetFrameRate;
            hudText.text = String.Format(formatString, optionData.displayName);
        }
    }

    public void IncrementAndApplyFrameRate()
    {
        ++currentRateIndex;
        if (frameRateOptions.Count <= currentRateIndex)
        {
            currentRateIndex = 0;
        }

        ApplyCurrentlySelectedRate();
    }
}
