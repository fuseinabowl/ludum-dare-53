using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownStation : MonoBehaviour
{
    public GameObject front;

    [HideInInspector]
    public Vector3 rootVertex;

    private void Start()
    {
        SingletonProvider.Get<VertexNetwork>().AddTownStation(this);
        SingletonProvider.Get<TownStatuses>().RegisterTown(this);
    }

    public void OnConnected()
    {
        SingletonProvider.Get<TownStatuses>().OnTownIsFedChanged(this, true);
    }

    public void OnDisconnected()
    {
        SingletonProvider.Get<TownStatuses>().OnTownIsFedChanged(this, false);
    }
}
