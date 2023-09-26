using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFX : MonoBehaviour
{
    [Header("SFX")]
    public FMODUnity.EventReference trackCompleteKnocks;
    public FMODUnity.StudioEventEmitter trackComplete;
    public FMODUnity.StudioEventEmitter trackPlaced;
    public FMODUnity.StudioEventEmitter trackDeleted;
    public FMODUnity.EventReference trackHover;
    public FMODUnity.StudioEventEmitter winJingle;

    [Header("Ambience")]
    public FMODUnity.EventReference birds;
    public FMODUnity.EventReference wind;

    public static SFX singleton
    {
        get { return SingletonProvider.Get<SFX>(); }
        private set { }
    }

    public void DidNextLevel()
    {
        Debug.Log("NextLevel");
        SingletonProvider.Get<FMODLoader>().music.setParameterByName("NextLevel", 1);
        SingletonProvider.Get<AdaptiveMusic>().SetParameter("NextLevel", 1);
    }

    public void DidGameOver()
    {
        Timer.Find(gameObject, "NextLevel").StartTimer();
    }

    public static void Play(
        FMODUnity.StudioEventEmitter emitter,
        string param1 = "",
        float value1 = 0f,
        string param2 = "",
        float value2 = 0f,
        string param3 = "",
        float value3 = 0f
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
        if (param3 != "")
        {
            emitter.SetParameter(param3, value3);
        }
    }

    public static void PlayOneShot(
        GameObject gameObject,
        FMODUnity.EventReference eventRef,
        string param1 = "",
        float value1 = 0,
        string param2 = "",
        float value2 = 0,
        string param3 = "",
        float value3 = 0
    )
    {
        Start(eventRef, gameObject, param1, value1, param2, value2, param3, value3).release();
    }

    public static FMOD.Studio.EventInstance Start(
        FMODUnity.EventReference eventRef,
        GameObject gameObject,
        string param1 = "",
        float value1 = 0,
        string param2 = "",
        float value2 = 0,
        string param3 = "",
        float value3 = 0
    )
    {
        var inst = CreateEventInstance(
            gameObject,
            eventRef,
            param1,
            value1,
            param2,
            value2,
            param3,
            value3
        );
        inst.start();
        return inst;
    }

    public static FMOD.Studio.EventInstance CreateEventInstance(
        GameObject gameObject,
        FMODUnity.EventReference eventRef,
        string param1 = "",
        float value1 = 0f,
        string param2 = "",
        float value2 = 0f,
        string param3 = "",
        float value3 = 0f
    )
    {
        var inst = FMODUnity.RuntimeManager.CreateInstance(eventRef);
        FMODUnity.RuntimeManager.AttachInstanceToGameObject(inst, gameObject.transform);
        inst.setTimelinePosition(0);
        if (param1 != "")
        {
            inst.setParameterByName(param1, value1);
        }
        if (param2 != "")
        {
            inst.setParameterByName(param2, value2);
        }
        if (param3 != "")
        {
            inst.setParameterByName(param3, value3);
        }
        return inst;
    }

    private void OnTimerTick(Timer timer)
    {
        switch (timer.timerName)
        {
            case "Ambience":
                var fmod = SingletonProvider.Get<FMODLoader>();
                fmod.GetAmbienceInstance(birds)
                    .setParameterByName("BirdsVolume", Mathf.Sqrt(Random.Range(0f, 1f)));
                fmod.GetAmbienceInstance(wind)
                    .setParameterByName("WindSpeed", Mathf.Pow(Random.Range(0f, 1f), 2f));
                break;
            case "NextLevel":
                DidNextLevel();
                break;
        }
    }
}
