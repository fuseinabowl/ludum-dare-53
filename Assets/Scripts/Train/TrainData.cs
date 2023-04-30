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
}
