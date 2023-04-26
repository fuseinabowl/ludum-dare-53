using UnityEngine;

public class Vectors
{
    /// <summary>
    /// Scales each x,y,z value in `vector` by the x,y,z values in `scale`.
    /// </summary>
    public static Vector3 Scale(Vector3 vector, Vector3 scale)
    {
        return new Vector3(vector.x * scale.x, vector.y * scale.y, vector.z * scale.z);
    }

    /// <summary>
    /// Return a vector with x component and 0 all other components.
    /// </summary>
    public static Vector3 X(float x)
    {
        return new Vector3(x, 0, 0);
    }

    /// <summary>
    /// Returns a vector with x component and other components taken from a
    /// different vector.
    /// </summary>
    public static Vector3 X(float x, Vector3 vector)
    {
        return new Vector3(x, vector.y, vector.z);
    }

    /// <summary>
    /// Return a vector with y component and 0 all other components.
    /// </summary>
    public static Vector3 Y(float y)
    {
        return new Vector3(0, y, 0);
    }

    /// <summary>
    /// Returns a vector with y component and other components taken from a
    /// different vector.
    /// </summary>
    public static Vector3 Y(float y, Vector3 vector)
    {
        return new Vector3(vector.x, y, vector.z);
    }

    /// <summary>
    /// Return a vector with z component and 0 all other components.
    /// </summary>
    public static Vector3 Z(float z)
    {
        return new Vector3(0, 0, z);
    }

    /// <summary>
    /// Returns a vector with z component and other components taken from a
    /// different vector.
    /// </summary>
    public static Vector3 Z(float z, Vector3 vector)
    {
        return new Vector3(vector.x, vector.y, z);
    }

    /// <summary>
    /// Returns `vector` with its z component set to 0.
    /// </summary>
    public static Vector3 Z0(Vector3 vector)
    {
        return new Vector3(vector.x, vector.y, 0f);
    }
}
