using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class PropSpawner : MonoBehaviour
{
    [SerializeField]
    private PropSpawnerOptions options;

    private void Start()
    {
        Spawn();
        Destroy(this);
    }

    private void Spawn()
    {
        if (ChooseWhetherToSpawn())
        {
            var prefab = ChooseSpawnPrefab();
            var spawnedObject = GameObject.Instantiate(prefab, transform);
            spawnedObject.transform.position = ChooseNearbyPosition();
            spawnedObject.transform.rotation = spawnedObject.transform.rotation * ChooseRandomAroundYAxisRotation();
        }
    }

    private bool ChooseWhetherToSpawn()
    {
        if (options.spawnChoices.Count == 0)
        {
            return false;
        }

        return Random.Range(0f, 1f) < options.spawnChance;
    }

    private GameObject ChooseSpawnPrefab()
    {
        Assert.AreNotEqual(options.spawnChoices.Count, 0, "No choices available. Call ChooseWhetherToSpawn() before ChooseSpawnPrefab() to avoid this.");
        var totalWeight = options.spawnChoices.Aggregate(0f, (runningTotal, choice) => runningTotal + choice.weight);
        var choiceWeight = Random.Range(0f, totalWeight);

        foreach (var choice in options.spawnChoices)
        {
            choiceWeight -= choice.weight;

            if (choiceWeight <= 0f)
            {
                return choice.prefab;
            }
        }

        return options.spawnChoices.Last().prefab;
    }

    /// <summary>
    /// Get nearby position
    /// </summary>
    /// <returns>Position in world space</returns>
    private Vector3 ChooseNearbyPosition()
    {
        return transform.position;
    }

    private Quaternion ChooseRandomAroundYAxisRotation()
    {
        return Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up);
    }
}
