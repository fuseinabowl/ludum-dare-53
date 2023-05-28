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

        // When the 4th town is fed, and every 3 towns after that, trigger a music transition.
        // When all towns are fed, musical transitions will be triggered every time the "NextLevel"
        // timer ticks.
        var fedTowns = FedTownsCount();
        if (fedTowns >= 4 && (fedTowns - 4) % 3 == 0)
        {
            SFX.singleton.DidNextLevel();
        }

        if (fedTowns == townIsFed.Count)
        {
            GameController.singleton.DidGameOver();
            SFX.singleton.DidGameOver();
        }
    }

    private void UpdateHud()
    {
        hudText.text = String.Format(formatString, townIsFed.Count, FedTownsCount());
    }

    private int FedTownsCount()
    {
        return townIsFed.Count(townRecord => townRecord.Value);
    }
}
