using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public TextMeshProUGUI hudText;
    public List<TextMeshProUGUI> creditsTexts;
    public float timeToFadeIn = 0.5f;
    public float creditsTimeToFadeIn = 0.5f;
    public float timeToFadePause = 1f;
    public float creditsTimeToFadePause = 3f;
    public float timeToFadeOut = 0.5f;

    [HideInInspector]
    public bool gameOver = false;

    public static GameController singleton
    {
        get { return SingletonProvider.Get<GameController>(); }
        private set { }
    }

    void Start()
    {
        hudText.alpha = 0f;
        foreach (var ui in creditsTexts)
        {
            ui.alpha = 0;
        }

        StartCoroutine(FadeInOut(hudText, 0, timeToFadeIn, timeToFadePause));
    }

    void Update() { }

    public void DidGameOver()
    {
        gameOver = true;
        SingletonProvider.Get<FreeFollowCamera>().enableFollowCamera = true;

        SFX.singleton.winJingle.Play();

        float pauseBefore = 0;
        foreach (var ui in creditsTexts)
        {
            StartCoroutine(FadeInOut(ui, pauseBefore, creditsTimeToFadeIn, creditsTimeToFadePause));
            pauseBefore += creditsTimeToFadeIn;
        }
    }

    private IEnumerator FadeInOut(
        TextMeshProUGUI ui,
        float pauseBefore,
        float fadeIn,
        float pauseDuring
    )
    {
        float delta = 0;
        while (delta < pauseBefore)
        {
            delta += Time.deltaTime;
            yield return null;
        }

        delta = 0;
        while (delta < fadeIn)
        {
            ui.alpha = delta / fadeIn;
            delta += Time.deltaTime;
            yield return null;
        }

        delta = 0;
        while (delta < pauseDuring)
        {
            delta += Time.deltaTime;
            yield return null;
        }

        delta = 0;
        while (delta < timeToFadeOut)
        {
            ui.alpha = 1 - (delta / timeToFadeOut);
            delta += Time.deltaTime;
            yield return null;
        }

        ui.alpha = 0f;
    }
}
