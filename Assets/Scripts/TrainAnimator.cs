using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainAnimator : MonoBehaviour
{
    public void OnTrainTurnStart()
    {
        SFX.PlayOneShot(gameObject, SFX.singleton.trainTurnStart);
    }

    public void OnTrainTurnEnd()
    {
        SFX.PlayOneShot(gameObject, SFX.singleton.trainTurnEnd);
    }
}
