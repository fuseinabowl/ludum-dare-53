using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// TODO: rename this to AdaptiveMusicClip? / AdaptiveMusicSequencer ???
/// standardise on names at least
///
/// TODO: Warn if there is no AudioListener / check if Unity's native audio is enabled
///
/// TODO: features for FMOD "parity enough":
///     - fade out on scene change using DontDestroyOnLoad / re-parenting to the root
///         - maybe this needs to just be done on the whole AdaptiveMusic class...
///           in fact it should probably be implemented like FMODStudioEventEmitter
///     - interrupt for transitions when parameters change (with quantisation)
///     - interrupt for (un)muting tracks when parameters change
///     - volume mixing (in real time)
/// </summary>
public class AdaptiveMusic : MonoBehaviour
{
    [SerializeField]
    AdaptiveMusicSequence startSequence;

    [SerializeField]
    AudioMixerGroup mixer;

    AdaptiveMusicSequence currentSequence;

    Dictionary<AudioClip, AudioSource> activeSources = new Dictionary<AudioClip, AudioSource>();

    Dictionary<AudioClip, AudioSource> sources1 = new Dictionary<AudioClip, AudioSource>();

    Dictionary<AudioClip, AudioSource> sources2 = new Dictionary<AudioClip, AudioSource>();

    Dictionary<AdaptiveMusicClip, MusicClipData> musicClipsData =
        new Dictionary<AdaptiveMusicClip, MusicClipData>();

    Dictionary<string, float> parameters = new Dictionary<string, float>();

    bool paused;

    public void SetParameter(string name, float value)
    {
        parameters[name] = value;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            if (currentSequence == null)
            {
                // Audio is initialised on application focus not component start because webgl
                // deprioritises background audio. Avoid playing audio then.
                currentSequence = startSequence;
                CreateAudioSourcesForCurrentSequence();
                StartCoroutine(Loop());
            }

            SetPaused(false);
        }
        else if (currentSequence != null)
        {
            SetPaused(true);
        }
    }

    void SetPaused(bool paused)
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            return;
        }

        this.paused = paused;

        foreach (var source in sources1)
        {
            SetAudioSourcePaused(source.Value, paused);
        }

        foreach (var source in sources2)
        {
            SetAudioSourcePaused(source.Value, paused);
        }
    }

    static void SetAudioSourcePaused(AudioSource audioSource, bool paused)
    {
        if (paused)
        {
            audioSource.Pause();
        }
        else
        {
            audioSource.UnPause();
        }
    }

    void CreateAudioSourcesForCurrentSequence()
    {
        foreach (var clip in currentSequence.clips)
        {
            AddClips(sources1, clip, true);
            AddClips(sources2, clip); // TODO: Only need to do this for sources that loop.
        }
    }

    void AddClips(
        Dictionary<AudioClip, AudioSource> sources,
        AdaptiveMusicClip musicClip,
        bool active = false
    )
    {
        sources[musicClip.clip] = CreateAudioSource(musicClip.clip);

        if (active)
        {
            musicClipsData[musicClip] = new MusicClipData(musicClip);
            activeSources[musicClip.clip] = sources[musicClip.clip];
        }

        foreach (var altClip in musicClip.alternateClips)
        {
            if (altClip != null)
            {
                sources[altClip] = CreateAudioSource(altClip);

                if (active)
                {
                    activeSources[altClip] = sources[altClip];
                }
            }
        }
    }

    IEnumerator Loop()
    {
        int loopIteration = 0;

        while (true)
        {
            while (paused)
            {
                yield return null;
            }

            float beatDuration = 60f / currentSequence.bpm;
            float barDuration = beatDuration * currentSequence.beatsPerBar;
            float loopDuration = barDuration * currentSequence.bars;
            float playhead = 0f;

            if (loopDuration == 0f)
            {
                Debug.LogWarningFormat(
                    "Loop length is 0, does sequence {0} have a valid tempo, length, and beats per bar?",
                    currentSequence.name
                );
                break;
            }

            foreach (var param in currentSequence.clearParameters)
            {
                parameters.Remove(param);
            }

            foreach (var activeSource in activeSources)
            {
                activeSource.Value.Stop();
            }

            // Copy keys because we need to modify the dictionary values, and C# doesn't let you do
            // that during a foreach loop.
            var audioClips = activeSources.Keys.ToList();

            foreach (var audioClip in audioClips)
            {
                var musicClip = FindMusicClip(audioClip);

                // musicClip can be null if the source belongs to a previous sequence.
                // Let this play out by itself.
                if (musicClip == null)
                {
                    continue;
                }

                var musicClipData = musicClipsData[musicClip];
                var audioSource = activeSources[audioClip];

                audioSource.mute =
                    !CheckCondition(musicClip.condition) || !musicClipData.IsPlayingClip(audioClip);
                audioSource.time = playhead;

                if (musicClip.beatOffset > 0)
                {
                    // TODO: Must check that if paused then this starts off paused; in fact,
                    // need to make sure that the delay pauses too. Perhaps this should be
                    // implemented as a Coroutine ourselves.
                    audioSource.PlayDelayed(musicClip.beatOffset * beatDuration);
                }
                else
                {
                    // TODO: Must check that if paused then this starts off paused.
                    audioSource.Play();
                }

                // Switch between double buffered audio so that the reverb
                // tail from one source plays while the other starts.
                if (sources1.ContainsValue(audioSource))
                {
                    activeSources[audioClip] = sources2[audioClip];
                }
                else
                {
                    activeSources[audioClip] = sources1[audioClip];
                }
            }

            while (playhead < loopDuration)
            {
                yield return null;

                if (!paused)
                {
                    playhead += Time.deltaTime;
                }
            }

            playhead -= loopDuration;

            foreach (var musicClip in currentSequence.clips)
            {
                musicClipsData[musicClip].NextIteration();
            }

            AdaptiveMusicSequence transitionSequence = null;

            foreach (var transition in currentSequence.transitions)
            {
                if (CheckCondition(transition.condition))
                {
                    transitionSequence = transition.sequence;
                    break;
                }
            }

            if (transitionSequence == null)
            {
                loopIteration++;
            }
            else
            {
                currentSequence = transitionSequence;
                CreateAudioSourcesForCurrentSequence();
                loopIteration = 0;
            }
        }
    }

    AdaptiveMusicClip FindMusicClip(AudioClip clip)
    {
        foreach (var musicClip in currentSequence.clips)
        {
            if (musicClip.clip == clip)
            {
                return musicClip;
            }

            if (System.Array.IndexOf(musicClip.alternateClips, clip) != -1)
            {
                return musicClip;
            }
        }

        return null;
    }

    bool CheckCondition(AdaptiveMusicCondition condition)
    {
        if (condition.parameter == null || condition.parameter == "")
        {
            return true;
        }

        float parameterValue = parameters.ContainsKey(condition.parameter)
            ? parameters[condition.parameter]
            : float.NaN;

        switch (condition.op)
        {
            case AdaptiveMusicCondition.Op.Equals:
                return parameterValue == condition.value;
            case AdaptiveMusicCondition.Op.NotEquals:
                return parameterValue != condition.value;
        }

        return false;
    }

    AudioSource CreateAudioSource(AudioClip clip)
    {
        var audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.outputAudioMixerGroup = mixer;
        return audioSource;
    }
}

class MusicClipData
{
    AdaptiveMusicClip musicClip;

    int clipsIndex = 0;
    int clipsLength
    {
        get { return musicClip.alternateClips.Length + 1; }
    }

    int[] shuffle;
    int shuffleIndex = 0;

    public MusicClipData(AdaptiveMusicClip musicClip)
    {
        this.musicClip = musicClip;
        shuffle = new int[clipsLength];

        for (int i = 0; i < shuffle.Length; i++)
        {
            shuffle[i] = i;
        }

        switch (musicClip.playOrder)
        {
            case AdaptiveMusicClip.PlayOrder.Shuffle:
                Shuffle();
                clipsIndex = shuffle[0];
                shuffleIndex = 1;
                break;

            case AdaptiveMusicClip.PlayOrder.Randomise:
                clipsIndex = Random.Range(0, clipsLength);
                break;
        }
    }

    void Shuffle()
    {
        for (int i = 0; i < shuffle.Length - 1; i++)
        {
            int j = Random.Range(i, shuffle.Length);
            if (i != j)
            {
                int tmp = shuffle[i];
                shuffle[i] = shuffle[j];
                shuffle[j] = tmp;
            }
        }
    }

    public AudioClip CurrentClip()
    {
        if (clipsIndex == 0)
        {
            return musicClip.clip;
        }

        return musicClip.alternateClips[clipsIndex - 1];
    }

    public bool IsPlayingClip(AudioClip clip)
    {
        return CurrentClip() == clip;
    }

    public void NextIteration()
    {
        if (musicClip.alternateClips.Length == 0)
        {
            return;
        }

        switch (musicClip.playOrder)
        {
            case AdaptiveMusicClip.PlayOrder.Sequential:
                clipsIndex = (clipsIndex + 1) % clipsLength;
                break;

            case AdaptiveMusicClip.PlayOrder.Shuffle:
                clipsIndex = shuffle[shuffleIndex];
                shuffleIndex++;
                if (shuffleIndex == shuffle.Length)
                {
                    shuffleIndex = 0;
                    Shuffle();
                }
                break;

            case AdaptiveMusicClip.PlayOrder.Randomise:
                clipsIndex = Random.Range(0, clipsLength);
                break;
        }
    }
}
