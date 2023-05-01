using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TownStatuses : MonoBehaviour
{
    private Dictionary<Station, bool> townIsFed = new Dictionary<Station, bool>();

    public void RegisterTown(Station town)
    {
        townIsFed.Add(town, false);
    }

    public void OnTownIsFedChanged(Station town, bool isFed)
    {
        townIsFed[town] = isFed;
    }
}
