using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="Grid Data", menuName="Grid Data")]
public class GridData : ScriptableObject
{
    public GameObject defaultSpawnedModel;
    public int seed = 0;
    public int mapSize = 4;
    public float cellSide = 0.5f;

    public List<GameObject> matchingOrderPrefabOverrides = new List<GameObject>();
}
