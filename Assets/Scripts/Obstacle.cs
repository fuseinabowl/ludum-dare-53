using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    private Vector3 centerPos;

    public void SetVertices(Vector3[] verts, Vector3 centerPosition)
    {
        if (Application.isPlaying)
        {
            centerPos = centerPosition;
            var net = SingletonProvider.Get<VertexNetwork>();
            net.RemoveMidpointEdges(centerPosition);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(centerPos, 0.3f);
    }
}
