using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformAnimatorSet : MonoBehaviour
{
    public List<TransformAnimator> animators = new List<TransformAnimator>();

    public void Animate(bool startIfAnimating = false) {
        foreach (var anim in animators) {
            anim.Animate(startIfAnimating);
        }
    }
}
