using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FMODLoader : MonoBehaviour
{
    public delegate void OnLoadHandler(FMODLoader self);

    public bool disableMusic = false;

    [Header("Banks")]
    [FMODUnity.BankRef]
    public string musicBank;

    [FMODUnity.BankRef]
    public string ambienceBank;

    [Header("Events")]
    public FMODUnity.EventReference musicEvent;

    public FMODUnity.EventReference[] ambienceEvents;

    [Header("Configuration")]
    public uint webGLBufferLength = 2048;

    public bool pauseWebGLOnBlur = true;

    [HideInInspector]
    public bool loaded { get; private set; } = false;

    [HideInInspector]
    public event OnLoadHandler onLoaded;

    [HideInInspector]
    public FMOD.Studio.EventInstance music { get; private set; }

    [HideInInspector]
    public FMOD.Studio.EventInstance[] ambience { get; private set; }

    private bool hasFocus;

    public FMOD.Studio.EventInstance GetAmbienceInstance(FMODUnity.EventReference ambienceRef)
    {
        if (ambience != null)
        {
            for (int i = 0; i < ambience.Length; i++)
            {
                if (ambienceEvents[i].Guid == ambienceRef.Guid)
                {
                    return ambience[i];
                }
            }
        }
        return new FMOD.Studio.EventInstance();
    }

    private void Awake()
    {
        // Need to run in background so that music/ambience can be paused.
        Application.runInBackground = true;
    }

    private void Start()
    {
        if (RelevantBanksLoaded())
        {
            DidLoad();
        }
        else
        {
            StartCoroutine(LoadCoroutine());
        }

        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            uint dspBufferLength;
            int dspBufferCount;
            FMODUnity.RuntimeManager.CoreSystem.getDSPBufferSize(
                out dspBufferLength,
                out dspBufferCount
            );
            if (dspBufferLength != webGLBufferLength)
            {
                Debug.LogWarningFormat(
                    "WebGL buffer length is {0}, but it should be {1}. "
                        + "Fix this via the FMOD menu > Edit Settings > Platform Specific > + > WebGL. "
                        + "Then next to DSP Buffer Settings untick Auto and set DSP Buffer Length to {1}.",
                    dspBufferLength,
                    webGLBufferLength
                );
            }
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        this.hasFocus = hasFocus;
        if (hasFocus)
        {
            MaybeStartEvents();
        }
        SetPaused(!hasFocus);
    }

    /// <summary>
    /// Based on https://www.fmod.com/docs/2.02/unity/examples-async-loading.html
    /// </summary>
    private IEnumerator LoadCoroutine()
    {
        if (musicBank != "")
        {
            FMODUnity.RuntimeManager.LoadBank(musicBank, true);
        }

        if (ambienceBank != "")
        {
            FMODUnity.RuntimeManager.LoadBank(ambienceBank, true);
        }

        while (!RelevantBanksLoaded())
        {
            yield return null;
        }

        while (FMODUnity.RuntimeManager.AnySampleDataLoading())
        {
            yield return null;
        }

        DidLoad();
    }

    private bool RelevantBanksLoaded()
    {
        return (musicBank == "" || FMODUnity.RuntimeManager.HasBankLoaded(musicBank))
            && (ambienceBank == "" || FMODUnity.RuntimeManager.HasBankLoaded(ambienceBank));
    }

    private void DidLoad()
    {
        loaded = true;
        onLoaded?.Invoke(this);

        if (hasFocus)
        {
            MaybeStartEvents();
        }
    }

    private void MaybeStartEvents()
    {
        if (!RelevantBanksLoaded())
        {
            return;
        }

        if (!musicEvent.IsNull && !music.isValid() && !disableMusic)
        {
            music = SFX.Start(musicEvent, gameObject);
        }

        if (ambienceEvents != null && ambience == null)
        {
            ambience = new FMOD.Studio.EventInstance[ambienceEvents.Length];
            for (int i = 0; i < ambience.Length; i++)
            {
                if (!ambienceEvents[i].IsNull)
                {
                    ambience[i] = SFX.Start(ambienceEvents[i], gameObject);
                }
            }
        }
    }

    private void SetPaused(bool paused)
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer || !pauseWebGLOnBlur)
        {
            return;
        }

        if (music.isValid())
        {
            music.setPaused(paused);
        }

        if (ambience != null)
        {
            foreach (var ambienceInstance in ambience)
            {
                if (ambienceInstance.isValid())
                {
                    ambienceInstance.setPaused(paused);
                }
            }
        }
    }
}
