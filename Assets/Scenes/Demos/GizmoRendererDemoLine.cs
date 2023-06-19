using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmoRendererLine : MonoBehaviour
{
    public GizmoRenderer.Shape shape = GizmoRenderer.Shape.LINE;
    public GizmoRenderer.Space space = GizmoRenderer.Space.WORLD;
    public GameObject target;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("must have target!");
        }
    }

    private void OnDrawGizmos()
    {
        if (target == null)
        {
            return;
        }

        var renderer = GetComponent<GizmoRenderer>();

        if (shape == GizmoRenderer.Shape.LINE)
        {
            renderer.DrawLine(transform.position, target.transform.position, space: space);
        }
        else if (shape == GizmoRenderer.Shape.LINE_ARROW)
        {
            renderer.DrawLine(
                transform.position,
                target.transform.position,
                arrow: true,
                space: space
            );
        }
    }
}
