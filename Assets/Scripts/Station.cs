using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Station : MonoBehaviour
{
    public enum Type { FARM, TOWN }
    public Type type = Type.FARM;
    public GameObject front;

    [HideInInspector]
    public VertexPath stationPath;
}
