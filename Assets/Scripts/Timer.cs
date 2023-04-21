using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A `Timer` component that keeps time internally and calls an `OnTimerTick` method on its
/// gameObject every `period` seconds.
/// 
/// Must be started manually with `StartTimer` unless `autoStart` is true.
/// 
/// If the frame rate is less than the timer period, `OnTimerTick` will be called once per frame.
/// </summary>
public class Timer : MonoBehaviour
{
    public string timerName = "";

    [Range(0.01f, 10f)] public float period = 1;

    public bool autoStart = false;

    public float nextTick { get; private set; } = float.PositiveInfinity;

    public bool running
    {
        get { return !float.IsPositiveInfinity(nextTick); }
        set { }
    }

    /// <summary>
    /// Finds the Timer with name `timerName` on `gameObject`.
    /// This method should only be used to find an existing `Timer`, use `CreateTimer` to create a new one.
    /// </summary>
    public static Timer FindTimer(GameObject gameObject, string timerName)
    {
        foreach (var timer in gameObject.GetComponents<Timer>())
        {
            if (timer.timerName == timerName)
            {
                return timer;
            }
        }
        Debug.LogWarningFormat("Timer \"{0}\" not found on {1}, creating new Timer", timerName, gameObject.name);
        return CreateTimer(gameObject, timerName);
    }

    /// <summary>
    /// Creates a new `Timer` and adds it to `gameObject`. If `period` isn't given then uses the default period.
    /// </summary>
    public static Timer CreateTimer(GameObject gameObject, string timerName, float period = -1f)
    {
        var newTimer = gameObject.AddComponent<Timer>();
        newTimer.timerName = timerName;
        if (period != -1f)
        {
            newTimer.period = Mathf.Clamp(period, 0.01f, 10f);
        }
        return newTimer;
    }

    public void StartTimer()
    {
        nextTick = period;
    }

    public void StopTimer()
    {
        nextTick = float.PositiveInfinity;
    }

    private void Start()
    {
        if (autoStart)
        {
            StartTimer();
        }
    }

    private void Update()
    {
        if (!float.IsPositiveInfinity(nextTick))
        {
            nextTick -= Time.deltaTime;
            if (nextTick < 0)
            {
                nextTick = Mathf.Max(nextTick + period, 0);
                SendMessage("OnTimerTick", this, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
