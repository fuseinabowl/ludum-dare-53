using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Poses
{
    public static Pose FromTransform(Transform transform)
    {
        return new Pose(transform.position, transform.rotation);
    }

    public static Pose Reverse(Pose pose)
    {
        return new Pose(pose.position, Quaternion.FromToRotation(pose.forward, -pose.forward));
    }
}
