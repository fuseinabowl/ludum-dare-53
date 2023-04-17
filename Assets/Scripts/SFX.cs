using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class SFX
{
    public static void PlayOneShot(
        EventReference fmodEvent,
        GameObject gameObject,
        string param1 = "",
        float value1 = 0,
        string param2 = "",
        float value2 = 0,
        string param3 = "",
        float value3 = 0
    )
    {
        Start(fmodEvent, gameObject, param1, value1, param2, value2, param3, value3).release();
    }

    public static EventInstance Start(
        EventReference fmodEvent,
        GameObject gameObject,
        string param1 = "",
        float value1 = 0,
        string param2 = "",
        float value2 = 0,
        string param3 = "",
        float value3 = 0
    )
    {
        var inst = Create(fmodEvent, gameObject, param1, value1, param2, value2, param3, value3);
        inst.start();
        return inst;
    }

    public static EventInstance Create(
        EventReference fmodEvent,
        GameObject gameObject,
        string param1 = "",
        float value1 = 0,
        string param2 = "",
        float value2 = 0,
        string param3 = "",
        float value3 = 0
    )
    {
        if (fmodEvent.IsNull)
        {
            Debug.LogError("Cannot start fmod event, event is null");
            return new EventInstance();
        }
        var inst = RuntimeManager.CreateInstance(fmodEvent);
        RuntimeManager.AttachInstanceToGameObject(inst, gameObject.transform);
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

    public static void StopRelease(
        EventInstance fmodInst,
        FMOD.Studio.STOP_MODE stopMode = FMOD.Studio.STOP_MODE.ALLOWFADEOUT
    )
    {
        fmodInst.stop(stopMode);
        fmodInst.release();
    }
}

