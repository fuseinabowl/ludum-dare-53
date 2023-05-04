using UnityEngine;

public class Vectors
{
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

    /// <summary>
    /// Gets vector with the absolute value of each component of `vector`.
    /// </summary>
    public static Vector3 Abs(Vector3 vector)
    {
        return new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
    }

    /// <summary>
    /// Gets vector with each component of `vector` rounded.
    /// </summary>
    public static Vector2 Round(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x),
            Mathf.Round(position.y),
            Mathf.Round(position.z)
        );
    }

    public static int Compare(Vector3 left, Vector3 right)
    {
        if (left == right)
        {
            return 0;
        }
        if (Mathf.Abs(left.x - right.x) > float.Epsilon)
        {
            return left.x < right.x ? -1 : 1;
        }
        if (Mathf.Abs(left.y - right.y) > float.Epsilon)
        {
            return left.y < right.y ? -1 : 1;
        }
        if (Mathf.Abs(left.z - right.z) > float.Epsilon)
        {
            return left.z < right.z ? -1 : 1;
        }
        return 1;
    }

    /// <summary>
    /// From https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/HandleUtility.cs#L115-L134
    /// </summary>
    public static Vector3 ProjectPointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 relativePoint = point - lineStart;
        Vector3 lineDirection = lineEnd - lineStart;
        float length = lineDirection.magnitude;
        Vector3 normalizedLineDirection = lineDirection;
        if (length > .000001f)
        {
            normalizedLineDirection /= length;
        }

        float dot = Vector3.Dot(normalizedLineDirection, relativePoint);
        dot = Mathf.Clamp(dot, 0.0F, length);

        return lineStart + normalizedLineDirection * dot;
    }
}
