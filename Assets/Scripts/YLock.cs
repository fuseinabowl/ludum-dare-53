using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YLock : MonoBehaviour
{
    public float y;

    // Start is called before the first frame update
    void Start()
    {
        transform.position = Pos();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Pos();
    }

    Vector3 Pos()
    {
        var parentPos = transform.parent.position;
        return new Vector3(parentPos.x, y, parentPos.z);
    }
}
