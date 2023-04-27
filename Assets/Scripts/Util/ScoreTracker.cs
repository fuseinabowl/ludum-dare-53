using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks a score during gameplay, and can save and load a history of them from prefs.
/// 
/// Scores are namespaced so that you can have multiple ScoreSystems for different scores,
/// for example a game might have "coins" and "bonus points". These would have a ScoreSystem each.
/// </summary>
public class ScoreTracker : MonoBehaviour
{
    public string key = "";

    public bool saveInPrefs = false;

    [Header("Debug")]
    public bool clearPrefsOnStart = false;

    [HideInInspector]
    public float highScore { get; private set; } = 0;

    private List<PrefScore> savedPrefScores = new List<PrefScore>();
    private List<PrefScore> unsavedPrefScores = new List<PrefScore>();

    /// <summary>
    /// Commits a score to memory and queues it to be saved in prefs when Save is called.
    /// Use `save = false` to save all scores immediately.
    /// </summary>
    public void CommitScore(float score, bool save = false)
    {
        unsavedPrefScores.Add(new PrefScore { score = score, timestamp = DateTime.Now.Ticks });
        if (score > highScore)
        {
            highScore = score;
        }
        if (save)
        {
            Save();
        }
    }

    /// <summary>
    /// Saves all unsaved scores. Alternatively, call CommitScore with save = true.
    /// </summary>
    public void Save()
    {
        if (!CanSave())
        {
            return;
        }

        if (unsavedPrefScores.Count > 0)
        {
            var allScores = new List<PrefScore>(savedPrefScores.Count + unsavedPrefScores.Count);
            allScores.AddRange(savedPrefScores);
            allScores.AddRange(unsavedPrefScores);

            var prefScores = new PrefScores { scores = allScores.ToArray() };
            PlayerPrefs.SetString(PrefKey(), JsonUtility.ToJson(prefScores));
            PlayerPrefs.Save();

            savedPrefScores = allScores;
            unsavedPrefScores = new List<PrefScore>();
        }
    }

    /// <summary>
    /// Loads the scores for `key` into memory. Typically you won't need to call this, the scores
    /// will automatically be loaded in Start, but it can be useful if you want to load earlier.
    /// </summary>
    public void Load()
    {
        if (!CanSave())
        {
            return;
        }

        if (unsavedPrefScores.Count > 0)
        {
            Debug.LogWarning("There are unsaved scores, these must be saved before loading");
            unsavedPrefScores = new List<PrefScore>();
        }

        savedPrefScores = new List<PrefScore>();

        if (PlayerPrefs.HasKey(PrefKey()))
        {
            var prefScoresJson = PlayerPrefs.GetString(PrefKey(), "");
            try
            {
                var prefScores = JsonUtility.FromJson<PrefScores>(prefScoresJson);
                savedPrefScores = new List<PrefScore>(prefScores.scores);
            }
            catch (Exception) { }
        }

        foreach (var prefScore in savedPrefScores)
        {
            if (prefScore.score > highScore)
            {
                highScore = prefScore.score;
            }
        }
    }

    /// <summary>
    /// Prints the high score and a history of all scores to Debug.Log.
    /// </summary>
    public void PrintDebug()
    {
        string s = string.Format("ScoreTracker for {0}:\n", key);
        s += string.Format("  High score: {0}\n", highScore);
        s += string.Format("  Saved {0} entries:\n", savedPrefScores.Count);
        foreach (var score in savedPrefScores)
        {
            s += string.Format("    {0} at {1}\n", score.score, new DateTime(score.timestamp));
        }
        s += string.Format("  Unsaved {0} entries:\n", unsavedPrefScores.Count);
        foreach (var score in unsavedPrefScores)
        {
            s += string.Format("    {0} at {1}\n", score.score, new DateTime(score.timestamp));
        }
        Debug.Log(s);
    }

    private void Start()
    {
        if (CanSave())
        {
            if (clearPrefsOnStart)
            {
                PlayerPrefs.DeleteKey(PrefKey());
            }

            Load();
        }
    }

    private void OnDestroy()
    {
        if (unsavedPrefScores.Count > 0)
        {
            Debug.LogWarningFormat("There are {0} unsaved scores for {1}", unsavedPrefScores.Count, key);
        }
    }

    private bool CanSave()
    {
        if (!saveInPrefs)
        {
            return false;
        }

        if (key == "")
        {
            Debug.LogWarning("Cannot save scores with an empty key");
            return false;
        }

        return true;
    }

    private string PrefKey()
    {
        return "Score." + key;
    }

    [System.Serializable]
    private class PrefScore
    {
        public float score;
        public long timestamp;
    }

    [System.Serializable]
    private class PrefScores
    {
        public PrefScore[] scores;
    }
}
