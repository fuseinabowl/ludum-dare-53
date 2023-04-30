using UnityEngine;

public static class Bezier
{
    public struct BezierResults
    {
        public Vector3 point;
        public Vector3 tangent;
        public Vector3 crossZ; // normal when viewed from above
    }

    public static BezierResults Calculate(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        var position = Vector3.Lerp(
            Vector3.Lerp(start, control, t),
            Vector3.Lerp(control, end, t),
            t
        );

        var dpDt = start * (2f * t - 2f) + (2f * end - 4f * control) * t + 2f * control;
        var tangent = dpDt.normalized;

        return new BezierResults{
            point = position,
            tangent = tangent,
            crossZ = Vector3.Cross(Vector3.up, tangent),
        };
    }
}
