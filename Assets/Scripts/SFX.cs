using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFX : MonoBehaviour
{
    public FMODUnity.StudioEventEmitter trackComplete;
    public FMODUnity.StudioEventEmitter trackPlaced;
    public FMODUnity.StudioEventEmitter trackDeleted;
    public FMODUnity.StudioEventEmitter birds;
    public FMODUnity.StudioEventEmitter wind;
    public FMODUnity.StudioEventEmitter winJingle;

    public static SFX singleton
    {
        get { return SingletonProvider.Get<SFX>(); }
        private set { }
    }

    public static void Play(
        FMODUnity.StudioEventEmitter emitter,
        string param1 = "",
        float value1 = 0f,
        string param2 = "",
        float value2 = 0f
    )
    {
        emitter.Play();
        if (param1 != "")
        {
            emitter.SetParameter(param1, value1);
        }
        if (param2 != "")
        {
            emitter.SetParameter(param2, value2);
        }
    }

    private void OnTimerTick(Timer timer)
    {
        if (timer.timerName == "Ambience")
        {
            birds.SetParameter("BirdsVolume", Mathf.Sqrt(Random.Range(0f, 1f)));
            wind.SetParameter("WindSpeed", Mathf.Pow(Random.Range(0f, 1f), 2f));
        }
    }
}
