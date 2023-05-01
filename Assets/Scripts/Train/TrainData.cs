using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="Train Data", menuName="Train Data")]
public class TrainData : ScriptableObject
{
    public GameObject locomotive;
    public GameObject carriage;

    public float carriageLength = 1f;

    [Min(0)]
    public int numberOfCarriages = 3;

    public string locomotiveAnimatorShownName = "show";
    public string carriageAnimatorFilledName = "filled";

    public float timeBetweenHidingLocomotiveAndLoadingCarriages = 0.1f;
    public float timeBetweenLoadingCarriages = 0.1f;
    public float timeBetweenShowingLocomotiveAndStartingMoving = 0.1f;
}
