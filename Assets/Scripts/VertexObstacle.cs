using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexObstacle : MonoBehaviour
{
    private Vector3 centerPos;

    private void Start()
    {
        VertexNetwork.singleton.BlockClosestVertex(transform.position);
    }
}
