using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexObstacle : MonoBehaviour
{
    private Vector3 centerPos;

    private void Start()
    {
        var net = SingletonProvider.Get<VertexNetwork>();
        net.RemoveClosestVertex(transform.position);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(transform.position, 0.3f);
    }
}
