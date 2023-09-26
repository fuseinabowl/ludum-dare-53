using UnityEngine;

[CreateAssetMenu(
    fileName = "AdaptiveMusicSequence",
    menuName = "AdaptiveMusic/Sequence",
    order = 1
)]
public class AdaptiveMusicSequence : ScriptableObject
{
    public float bpm;
    public int bars;
    public int beatsPerBar;
    public AdaptiveMusicClip[] clips;
    public AdaptiveMusicTransition[] transitions;
    public string[] clearParameters;
    // TODO: volume (fader)
}

[System.Serializable]
public class AdaptiveMusicClip
{
    public enum PlayOrder
    {
        Sequential,
        Shuffle,
        Randomise,
    }

    public AudioClip clip;
    public AudioClip[] alternateClips;
    public PlayOrder playOrder;
    public AdaptiveMusicCondition condition;

    [Min(0)]
    public int beatOffset;
}

[System.Serializable]
public class AdaptiveMusicTransition
{
    public AdaptiveMusicSequence sequence;
    public AdaptiveMusicCondition condition;
    public bool interrupt;
    public int quantizeBars;
    public int quantizeBeats;
}

[System.Serializable]
public class AdaptiveMusicCondition
{
    public enum Op
    {
        Equals,
        NotEquals,
    }

    public string parameter;
    public Op op;
    public float value;
}
