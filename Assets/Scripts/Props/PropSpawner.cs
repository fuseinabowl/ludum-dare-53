using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class PropSpawner : MonoBehaviour
{
    [SerializeField]
    private PropSpawnerOptions options;

    public void Spawn(Matrix4x4 preWarpTransform, CageWarper warper)
    {
        if (ChooseWhetherToSpawn())
        {
            var prefab = ChooseSpawnPrefab();
            var spawnedObject = GameObject.Instantiate(prefab, transform.parent);
            var localPosition = ChooseNearbyPosition();
            var preWarpPosition = preWarpTransform.MultiplyPoint(localPosition);
            var warpedPosition = warper.WarpVertex(preWarpPosition);
            spawnedObject.transform.localPosition = warpedPosition;
            if (options.randomlyRotate)
            {
                spawnedObject.transform.rotation = spawnedObject.transform.rotation * ChooseRandomAroundYAxisRotation();
            }
            else
            {
                var forward = preWarpTransform.MultiplyVector(transform.localRotation * Vector3.forward);
                var warpedForward = warper.WarpNormal(transform.localPosition, forward);
                // not quite right. Need to add some more code to the warper to get this to line up with the geo instead
                // of the normals
                spawnedObject.transform.rotation = Quaternion.LookRotation(warpedForward, Vector3.up);
            }

            spawnedObject.hideFlags = HideFlags.DontSave;
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
    /// <returns>Position in local space</returns>
    private Vector3 ChooseNearbyPosition()
    {
        var topDownPosition = Random.insideUnitCircle * options.maxPositionVariation.x;
        var topDownPosition3d = new Vector3(topDownPosition.x, 0f, topDownPosition.y);
        var verticalOffset = Random.Range(-options.maxPositionVariation.y, options.maxPositionVariation.y);
        var offset = topDownPosition3d + Vector3.up * verticalOffset;
        return transform.localPosition + offset;
    }

    private Quaternion ChooseRandomAroundYAxisRotation()
    {
        return Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up);
    }
}
