using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A collection of animations that are programmed directly against the transform.
///
/// Pulse and spin are useful for objects.
/// Shake is useful for cameras, though could be used on objects too.
///
/// Multiple of these animations running together may interfere with each other, so
/// they will sequence themselves to avoid that.
/// </summary>
public class TransformAnimator : MonoBehaviour
{
    public enum Animation { PULSE, SPIN, SHAKE, }

    public new Animation animation = Animation.PULSE;

    [Range(0, 1)]
    public float duration = 0.1f;

    [Range(0, 2)]
    public float strength = 2f;

    [Range(0, 1)]
    public float frequency = 0.1f;

    public bool animateOnStart = false;

    // public bool loop = false;

    private bool animating = false;

    public void Animate()
    {
        StartCoroutine(IAnimate());
    }

    public IEnumerator IAnimate()
    {
        // Only allow a single animation at a time. This is a lazy solution.
        while (AnyComponentIsAnimating())
        {
            yield return null;
        }

        animating = true;

        switch (animation)
        {
            case Animation.PULSE:
                yield return IPulse();
                break;
            case Animation.SPIN:
                // yield return IShake();
                break;
            case Animation.SHAKE:
                yield return IShake();
                break;
        }

        animating = false;
    }

    private IEnumerator IPulse()
    {
        var startLocalScale = transform.localScale;

        float elapsed = 0;

        while (elapsed < duration)
        {
            transform.localScale = ForwardBackStep(elapsed) * startLocalScale;
            yield return null;
            elapsed += Time.deltaTime;
        }

        transform.localScale = startLocalScale;
    }

    private float ForwardBackStep(float elapsed)
    {
        float progress = elapsed * 2f;
        if (progress > duration)
        {
            progress = 2 * duration - progress;
        }
        return Mathf.SmoothStep(1f, strength, progress / duration);
    }

    private void Start()
    {
        if (animateOnStart)
        {
            Animate();
        }
    }

    private bool AnyComponentIsAnimating()
    {
        if (animating)
        {
            return true;
        }

        var animators = GetComponents<TransformAnimator>();
        if (animators.Length > 1)
        {
            foreach (var anim in animators)
            {
                if (anim.animating)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private IEnumerator IShake()
    {
        // This implementation doesn't make sense, I copied it from a previous LD and without the
        // original context it's completely wrong. Need to actually read this and then fix it:
        // https://docs.unity3d.com/ScriptReference/Mathf.PerlinNoise.html

        var seed = UnityEngine.Random.value;
        var startPosition = transform.localPosition;
        var startRotation = transform.localRotation;

        float elapsed = 0;

        while (elapsed < duration)
        {
            var step = ForwardBackStep(elapsed);
            transform.localPosition =
                new Vector3(
                    (Mathf.PerlinNoise(seed, Time.time * frequency) * 2 - 1),
                    (Mathf.PerlinNoise(seed + 1, Time.time * frequency) * 2 - 1),
                    (Mathf.PerlinNoise(seed + 2, Time.time * frequency) * 2 - 1)
                )
                * step
                * 0.5f;
            transform.localRotation = Quaternion.Euler(
                new Vector3(
                    (Mathf.PerlinNoise(seed + 3, Time.time * frequency) * 2 - 1),
                    (Mathf.PerlinNoise(seed + 4, Time.time * frequency) * 2 - 1),
                    (Mathf.PerlinNoise(seed + 5, Time.time * frequency) * 2 - 1)
                )
                    * step
                    * 0.2f
            );

            yield return null;
            elapsed += Time.deltaTime;
        }

        transform.localPosition = startPosition;
        transform.localRotation = startRotation;
    }
}