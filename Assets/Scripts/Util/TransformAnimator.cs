using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A collection of animations that are programmed directly against the transform.
/// Not all configuration options will apply to every animation.
///
/// Pulse and spin are useful for objects.
/// Shake is useful for cameras, though could be used on objects too.
///
/// These are all designed so that any number of animations can run concurrently, and
/// by the end they will have converged to the original transform.
/// </summary>
public class TransformAnimator : MonoBehaviour
{
    public enum Animation { PULSE, SPIN, SHAKE, WIGGLE }

    public new Animation animation = Animation.PULSE;

    [Range(0, 5)]
    public float duration = 1f;

    [Range(0, 2)]
    public float magnitude = 2f;

    [Range(0, 100)]
    public float frequency = 10f;

    // public bool continuous = false;

    public bool animateOnStart = false;

    private Vector3 startLocalPosition;
    private Vector3 startLocalScale;
    private Quaternion startLocalRotation;
    private TransformAnimator[] animators;
    private int animationCount = 0;

    public void Animate()
    {
        if (enabled)
        {
            StartCoroutine(IAnimate());
        }
    }

    public IEnumerator IAnimate()
    {
        if (!AnyComponentIsAnimating())
        {
            startLocalPosition = transform.localPosition;
            startLocalScale = transform.localScale;
            startLocalRotation = transform.localRotation;
        }

        animationCount++;

        switch (animation)
        {
            case Animation.PULSE:
                yield return IPulse();
                break;
            case Animation.SPIN:
                yield return ISpin();
                break;
            case Animation.SHAKE:
                yield return IShakeWiggle(true, false);
                break;
            case Animation.WIGGLE:
                yield return IShakeWiggle(false, true);
                break;
        }

        animationCount--;

        if (!AnyComponentIsAnimating())
        {
            transform.localPosition = startLocalPosition;
            transform.localScale = startLocalScale;
            transform.localRotation = startLocalRotation;
        }
    }

    private IEnumerator IPulse()
    {
        float elapsed = 0;
        float prevScale = 1f;

        while (elapsed < duration)
        {
            var currentScale = BiSmoothStep(1f, magnitude, elapsed / duration);
            transform.localScale *= 1 + currentScale - prevScale;
            prevScale = currentScale;
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    private IEnumerator ISpin()
    {
        float elapsed = 0;
        float prevRotation = 0f;

        while (elapsed < duration)
        {
            var currentRotation = Mathf.SmoothStep(0f, 360f, elapsed / duration);
            transform.localRotation = Quaternion.Euler(
                transform.localRotation.eulerAngles + (currentRotation - prevRotation) * Vector3.up
            );
            prevRotation = currentRotation;
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    private IEnumerator IShakeWiggle(bool shake, bool wiggle)
    {
        var seed = UnityEngine.Random.value;

        float elapsed = 0;
        var prevLocalPosition = Vector3.zero;
        var prevLocalRotation = Quaternion.identity.eulerAngles;

        while (elapsed < duration)
        {
            var smoothMagnitude = BiSmoothStep(0, magnitude, elapsed / duration);

            if (shake)
            {
                var currentLocalPosition = smoothMagnitude * new Vector3(
                    Mathf.PerlinNoise(seed, elapsed * frequency) * 2 - 1,
                    Mathf.PerlinNoise(seed + 1, elapsed * frequency) * 2 - 1,
                    Mathf.PerlinNoise(seed + 2, elapsed * frequency) * 2 - 1
                );
                transform.localPosition += currentLocalPosition - prevLocalPosition;
                prevLocalPosition = currentLocalPosition;
            }

            if (wiggle)
            {
                var currentLocalRotation = 15f * smoothMagnitude * new Vector3(
                    Mathf.PerlinNoise(seed + 3, elapsed * frequency) * 2 - 1,
                    Mathf.PerlinNoise(seed + 4, elapsed * frequency) * 2 - 1,
                    Mathf.PerlinNoise(seed + 5, elapsed * frequency) * 2 - 1
                );
                transform.localRotation = Quaternion.Euler(
                    transform.localRotation.eulerAngles + currentLocalRotation - prevLocalRotation
                );
                prevLocalRotation = currentLocalRotation;
            }

            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    private void Awake()
    {
        animators = GetComponents<TransformAnimator>();
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
        foreach (var anim in animators)
        {
            if (anim.animationCount > 0)
            {
                return true;
            }
        }
        return false;
    }

    private float BiSmoothStep(float from, float to, float t)
    {
        float t2 = t * 2f;
        if (t > 0.5f)
        {
            t2 = (1f - t) * 2f;
        }
        return Mathf.SmoothStep(from, to, t2);
    }
}
