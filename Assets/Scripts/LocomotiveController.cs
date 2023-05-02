using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocomotiveController : MonoBehaviour
{
    public FMODUnity.StudioEventEmitter chug;
    public FMODUnity.StudioEventEmitter loaded;
    public FMODUnity.StudioEventEmitter unloaded;

    private bool head = false;

    public void SetIsHead(bool isHead) {
        head = isHead;
        if (head) {
            chug.Play();
        }
    }

    public void DidPause()
    {
        chug.SetParameter("TrainSpeed", 0);
    }

    public void DidResume()
    {
        chug.SetParameter("TrainSpeed", 1);
    }

    public void DidLoad()
    {
        loaded.Play();
    }

    public void DidUnload()
    {
        unloaded.Play();
    }
}
