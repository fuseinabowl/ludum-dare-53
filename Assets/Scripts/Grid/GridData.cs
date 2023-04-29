using System;
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

    [Serializable]
    public class QuadOverride
    {
        public GameObject prefab = null;
        // 0-3
        public int rotationIndex = 0;
        public bool isFlippedAcrossX = false;
    }

    public List<QuadOverride> matchingOrderPrefabOverrides = new List<QuadOverride>();
}
