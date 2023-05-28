using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TownStatuses : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Supports {0} for total towns and {1} for fed towns")]
    private string formatString;

    [SerializeField]
    private TextMeshProUGUI hudText;

    private Dictionary<TownStation, bool> townIsFed = new Dictionary<TownStation, bool>();

    private void Start()
    {
        UpdateHud();
    }

    public void RegisterTown(TownStation town)
    {
        townIsFed.Add(town, false);
    }

    public void OnTownIsFedChanged(TownStation town, bool isFed)
    {
        townIsFed[town] = isFed;
        UpdateHud();

        // When the 4th town is fed, and every second town after that, trigger a music transition.
        var fedTowns = FedTownsCount();
        if (fedTowns >= 4 && fedTowns % 2 == 0)
        {
            SFX.singleton.NextLevel();
        }
    }

    private void UpdateHud()
    {
        var totalTowns = townIsFed.Count;
        var fedTowns = FedTownsCount();

        hudText.text = String.Format(formatString, totalTowns, fedTowns);

        if (totalTowns == fedTowns)
        {
            GameController.singleton.DidGameOver();
        }
    }

    private int FedTownsCount()
    {
        return townIsFed.Count(townRecord => townRecord.Value);
    }
}
