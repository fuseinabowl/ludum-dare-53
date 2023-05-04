using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainAudio : MonoBehaviour
{
    public FMODUnity.StudioEventEmitter chug;
    public FMODUnity.StudioEventEmitter loaded;
    public FMODUnity.StudioEventEmitter unloaded;

    public void pauseChug()
    {
        chug.SetParameter("TrainSpeed", 0);
    }

    public void resumeChug()
    {
        chug.SetParameter("TrainSpeed", 1);
    }
}
