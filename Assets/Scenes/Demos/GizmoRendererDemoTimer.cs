using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MouseHitTarget))]
[RequireComponent(typeof(GizmoRenderer))]
public class GizmoRendererTimer : MonoBehaviour
{
    [SerializeField]
    private GameObject target;

    [SerializeField]
    [Range(0, 5)]
    private float timer;

    [SerializeField]
    private bool arrow;

    private void OnMouseHitDown(MouseHitTarget.Event e)
    {
        GetComponent<GizmoRenderer>()
            .AddLine(
                transform.position,
                target.transform.position,
                arrow: arrow,
                space: GizmoRenderer.Space.WORLD,
                timer: 1f
            );
    }
}
