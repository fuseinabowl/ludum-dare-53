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

    [Range(0.01f, 120f)]
    public float period = 1;

    public bool autoStart = false;

    public bool oneShot = false;

    public float nextTick { get; private set; } = float.PositiveInfinity;

    public bool ticked
    {
        get { return nextTick == 0; }
        set { }
    }

    public bool running
    {
        get { return !float.IsPositiveInfinity(nextTick); }
        set { }
    }

    public float elapsed
    {
        get { return period - nextTick; }
        set { }
    }

    /// <summary>
    /// `event` interface to be notified on timer tick. Called at the same time as `OnTimerTick`.
    /// </summary>
    public event TickHandler tick;
    public delegate void TickHandler(Timer timer);

    private bool didUpdateThisFrame = false;
    private bool didTickThisUpdate = false;
    private bool deleteAfterTick = false;

    /// <summary>
    /// Finds the Timer with name `timerName` on `gameObject`.
    /// This method should only be used to find an existing `Timer`, use `CreateTimer` to create a new one.
    /// </summary>
    public static Timer Find(GameObject gameObject, string timerName)
    {
        foreach (var timer in gameObject.GetComponents<Timer>())
        {
            if (timer.timerName == timerName)
            {
                return timer;
            }
        }
        Debug.LogWarningFormat(
            "Timer \"{0}\" not found on {1}, creating new Timer",
            timerName,
            gameObject.name
        );
        return Create(gameObject, -1, timerName);
    }

    /// <summary>
    /// Creates a new `Timer` and adds it to `gameObject`.
    /// </summary>
    public static Timer Create(GameObject gameObject, float period, string timerName = null)
    {
        var newTimer = gameObject.AddComponent<Timer>();
        newTimer.period = Mathf.Clamp(period, 0.01f, 10f);
        if (timerName != null)
        {
            newTimer.timerName = timerName;
        }
        return newTimer;
    }

    /// <summary>
    /// Creates a new one-shot `Timer` and adds it to `gameObject`. One-shot timers created this
    /// way will fire once then delete themselves.
    /// </summary>
    public static Timer OneShot(GameObject gameObject, float period, string timerName = null)
    {
        var newTimer = Create(gameObject, period, timerName);
        newTimer.autoStart = true;
        newTimer.oneShot = true;
        newTimer.deleteAfterTick = true;
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

    /// <summary>
    /// Returns if this component will, or already has, called OnTimerTick this update.
    /// </summary>
    public bool WillTickThisFrame()
    {
        if (didUpdateThisFrame)
        {
            return didTickThisUpdate;
        }
        return nextTick - Time.deltaTime <= 0;
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
        didTickThisUpdate = false;
        didUpdateThisFrame = true;

        if (!float.IsPositiveInfinity(nextTick))
        {
            nextTick -= Time.deltaTime;
            if (nextTick <= 0)
            {
                nextTick = Mathf.Max(nextTick + period, 0);
                didTickThisUpdate = true;
                if (oneShot)
                {
                    // Disable before ticking to give listeners a chance to restart the one-shot.
                    nextTick = float.PositiveInfinity;
                }
                SendMessage("OnTimerTick", this, SendMessageOptions.DontRequireReceiver);
                tick?.Invoke(this);
                if (float.IsPositiveInfinity(nextTick) && deleteAfterTick)
                {
                    // Listener didn't restart the one-shot and the timer has been configured to
                    // delete itself (i.e. it was created programmatically in Timer.OneShot).
                    GameObject.Destroy(this);
                }
            }
        }
    }

    private void LateUpdate()
    {
        didUpdateThisFrame = false;
    }
}
