using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MouseHitTarget))]
[RequireComponent(typeof(TransformAnimator))]
public class TransitionAnimatorDemoHold : MonoBehaviour
{
    private void OnMouseHitHoverStart(MouseHitTarget.Event evt) {
        GetComponent<TransformAnimator>().Animate();
    }

    private void OnMouseHitHoverEnd(MouseHitTarget.Event evt) {
        GetComponent<TransformAnimator>().Break();
    }
}
