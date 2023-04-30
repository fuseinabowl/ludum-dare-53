using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="Train Data", menuName="Train Data")]
public class TrainData : ScriptableObject
{
    public GameObject locomotive;
    public GameObject carriage;

    [Tooltip("Distance between the centers of the cars")]
    public float separation = 1f;

    [Min(0)]
    public int numberOfCarriages = 3;
}
