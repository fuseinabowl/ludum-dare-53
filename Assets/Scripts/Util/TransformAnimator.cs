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
    public enum Animation
    {
        PULSE,
        PULSE_HOLD,
        SPIN,
        SHAKE,
        WIGGLE,
        NUDGE,
        NUDGE_HOLD
    }

    [System.Serializable]
    public class AnimationConfig
    {
        public bool enabled = true;
        public Animation animation = Animation.PULSE;
        public float magnitude = 2f;
        public float complexity = 10f;
        public Vector3 direction = Vector3.up;
    }

    public Transform otherTransform = null;

    [Header("Configuration")]
    public new Animation animation = Animation.PULSE;

    public float duration = 1f;

    public float magnitude = 2f;

    public float complexity = 10f;

    public Vector3 direction = Vector3.up;

    [Header("Multiple Animations")]
    public List<AnimationConfig> multipleAnimations = new List<AnimationConfig>();

    [Header("Behaviour")]
    public bool continuous = false;

    public bool animateOnStart = false;

    private Transform t
    {
        get { return otherTransform != null ? otherTransform : transform; }
        set { }
    }
    private Vector3 startLocalPosition;
    private Vector3 startLocalScale;
    private Quaternion startLocalRotation;
    private TransformAnimator[] animators;
    private int animationCount = 0;
    private bool restoreContinuous = false;

    public void Animate(bool startIfAnimating = false)
    {
        if (enabled && startIfAnimating)
        {
            StartCoroutine(
                IAnimate(
                    new AnimationConfig
                    {
                        animation = animation,
                        magnitude = magnitude,
                        complexity = complexity,
                        direction = direction,
                    }
                )
            );

            foreach (var conf in multipleAnimations)
            {
                if (conf.enabled)
                {
                    StartCoroutine(IAnimate(conf));
                }
            }
        }
    }

    public void Animate()
    {
        if ((animation == Animation.PULSE_HOLD || animation == Animation.NUDGE_HOLD) && !continuous)
        {
            Debug.LogWarning("Cannot animate held animations without continuous = true");
            return;
        }

        Animate(true);
    }

    /// <summary>
    /// Break the current continuously running animation and restore it to continuous afterwards.
    /// Especially helpful for held animations.
    /// </summary>
    public void Break()
    {
        if (AnyComponentIsAnimating() && continuous)
        {
            continuous = false;
            restoreContinuous = true;
        }
    }

    private IEnumerator IAnimate(AnimationConfig conf)
    {
        if (!AnyComponentIsAnimating())
        {
            startLocalPosition = t.localPosition;
            startLocalScale = t.localScale;
            startLocalRotation = t.localRotation;
        }

        animationCount++;

        switch (conf.animation)
        {
            case Animation.PULSE:
                yield return IPulse(conf);
                break;
            case Animation.PULSE_HOLD:
                yield return IPulseHold(conf);
                break;
            case Animation.SPIN:
                yield return ISpin(conf);
                break;
            case Animation.SHAKE:
                yield return IShakeWiggle(conf, true, false);
                break;
            case Animation.WIGGLE:
                yield return IShakeWiggle(conf, false, true);
                break;
            case Animation.NUDGE:
                yield return INudge(conf);
                break;
            case Animation.NUDGE_HOLD:
                yield return INudgeHold(conf);
                break;
        }

        animationCount--;

        if (!AnyComponentIsAnimating())
        {
            t.localPosition = startLocalPosition;
            t.localScale = startLocalScale;
            t.localRotation = startLocalRotation;

            if (restoreContinuous)
            {
                continuous = true;
            }
        }
    }

    private IEnumerator IPulse(AnimationConfig conf)
    {
        float elapsed = 0;
        float prevScale = 1f;

        while (continuous || elapsed < duration)
        {
            if (elapsed > duration)
            {
                elapsed -= duration;
            }

            var currentScale = BiSmoothStep(1f, conf.magnitude, elapsed / duration);
            t.localScale *= 1 + currentScale - prevScale;
            prevScale = currentScale;

            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    private IEnumerator IPulseHold(AnimationConfig conf)
    {
        float elapsed = 0;
        float prevScale = 1f;

        while (continuous)
        {
            var currentScale = BiSmoothStep(1f, conf.magnitude, elapsed / duration);
            t.localScale *= 1 + currentScale - prevScale;
            prevScale = currentScale;

            yield return null;
            elapsed += Time.deltaTime;

            if (elapsed > duration / 2f)
            {
                elapsed = duration / 2f;
            }
        }

        elapsed = duration - elapsed;

        while (elapsed < duration)
        {
            var currentScale = BiSmoothStep(1f, conf.magnitude, elapsed / duration);
            t.localScale *= 1 + currentScale - prevScale;
            prevScale = currentScale;

            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    private IEnumerator ISpin(AnimationConfig conf)
    {
        float elapsed = 0;
        float prevRotation = 0f;

        while (continuous || elapsed < duration)
        {
            float currentRotation = continuous
                ? (360f * elapsed / duration) % 360f
                : Mathf.SmoothStep(0f, 360f, elapsed / duration);

            t.localRotation = Quaternion.Euler(
                t.localRotation.eulerAngles + (currentRotation - prevRotation) * Vector3.up
            );

            prevRotation = currentRotation;
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    private IEnumerator IShakeWiggle(AnimationConfig conf, bool shake, bool wiggle)
    {
        var seed = UnityEngine.Random.value;

        float elapsed = 0;
        var prevLocalPosition = Vector3.zero;
        var prevLocalRotation = Quaternion.identity.eulerAngles;

        while (continuous || elapsed < duration)
        {
            var magnitudeStep = continuous
                ? conf.magnitude
                : BiSmoothStep(0, conf.magnitude, elapsed / duration);

            if (shake)
            {
                var currentLocalPosition =
                    5f
                    * magnitudeStep
                    * new Vector3(
                        Mathf.PerlinNoise(seed, elapsed * conf.complexity) * 2 - 1,
                        Mathf.PerlinNoise(seed + 1, elapsed * conf.complexity) * 2 - 1,
                        Mathf.PerlinNoise(seed + 2, elapsed * conf.complexity) * 2 - 1
                    );
                t.localPosition += currentLocalPosition - prevLocalPosition;
                prevLocalPosition = currentLocalPosition;
            }

            if (wiggle)
            {
                var currentLocalRotation =
                    15f
                    * magnitudeStep
                    * new Vector3(
                        Mathf.PerlinNoise(seed + 3, elapsed * conf.complexity) * 2 - 1,
                        Mathf.PerlinNoise(seed + 4, elapsed * conf.complexity) * 2 - 1,
                        Mathf.PerlinNoise(seed + 5, elapsed * conf.complexity) * 2 - 1
                    );
                t.localRotation = Quaternion.Euler(
                    t.localRotation.eulerAngles + currentLocalRotation - prevLocalRotation
                );
                prevLocalRotation = currentLocalRotation;
            }

            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    private IEnumerator INudge(AnimationConfig conf)
    {
        float elapsed = 0;
        float prevExtent = 0;

        while (continuous || elapsed < duration)
        {
            if (elapsed > duration)
            {
                elapsed -= duration;
            }

            var currentExtent = BiSmoothStep(0f, conf.magnitude, elapsed / duration);
            t.localPosition += (currentExtent - prevExtent) * direction.normalized;
            prevExtent = currentExtent;

            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    private IEnumerator INudgeHold(AnimationConfig conf)
    {
        float elapsed = 0;
        float prevExtent = 0;

        while (continuous)
        {
            var currentExtent = BiSmoothStep(0f, conf.magnitude, elapsed / duration);
            t.localPosition += (currentExtent - prevExtent) * direction.normalized;
            prevExtent = currentExtent;

            yield return null;
            elapsed += Time.deltaTime;

            if (elapsed > duration / 2f)
            {
                elapsed = duration / 2f;
            }
        }

        elapsed = duration - elapsed;

        while (elapsed < duration)
        {
            var currentExtent = BiSmoothStep(0f, conf.magnitude, elapsed / duration);
            t.localPosition += (currentExtent - prevExtent) * direction.normalized;
            prevExtent = currentExtent;

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
