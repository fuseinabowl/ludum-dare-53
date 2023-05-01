using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SphereGizmo : MonoBehaviour
{
    public Color color = Color.blue;
    public float radius = 1f;

    private void OnDrawGizmos() {
        Gizmos.color = color;
        Gizmos.DrawSphere(transform.position, radius);
    }
}
