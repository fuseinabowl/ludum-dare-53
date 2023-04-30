using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Prop Spawner Options", fileName="Prop Spawner Options")]
public class PropSpawnerOptions : ScriptableObject
{
    [Range(0f,1f)]
    public float spawnChance = 1f;

    /// <summary>
    /// X is used for both maximum X and maximum Z variation.
    /// Y is used for maximum Y variation.
    /// </summary>
    public Vector2 maxPositionVariation = Vector2.zero;

    [Serializable]
    public class SpawnChoice
    {
        public float weight = 1;
        public GameObject prefab;
    }

    public List<SpawnChoice> spawnChoices = new List<SpawnChoice>();
}
