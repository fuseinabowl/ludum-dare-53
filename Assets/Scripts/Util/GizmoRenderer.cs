using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GizmoRenderer : MonoBehaviour
{
    public enum Space
    {
        WORLD,
        LOCAL,
        CAMERA,
    }

    public enum Variant
    {
        PRIMARY,
        SECONDARY,
        TERTIARY,
    }

    public enum Shape
    {
        SPHERE,
        SPHERE_WIRE,
        CUBE,
        CUBE_WIRE,
        CUBE_WIRE_CORNERS,
        DIRECTION,
        DIRECTION_ARROW,
        LINE,
        LINE_ARROW,
    }

    [Header("Options")]
    public bool drawSelf = false;
    public bool drawPrimary = true;
    public bool drawSecondary = true;
    public bool drawTertiary = true;

    [Header("Colors")]
    public Color primary = Color.red;
    public Color secondary = Color.green;
    public Color tertiary = Color.blue;

    [Header("Sizes")]
    public Vector3 defaultSize = Vector3.one;

    [Range(0f, 1f)]
    public float sphereCornerSize = 0.03f;

    [Range(0f, 1f)]
    public float arrowheadSize = 0.1f;

    [Range(0f, 90f)]
    public float arrowheadAngle = 30f;

    [Range(0.01f, 1f)]
    public float cameraScale = 0.1f;

    [Header("Self")]
    public Variant selfVariant = Variant.PRIMARY;
    public Shape selfShape = Shape.SPHERE_WIRE;
    public Vector3 selfSize = Vector3.one;
    public Vector3 selfOffset = Vector3.zero;
    public Space selfSpace = Space.LOCAL;

    private List<GizmoData> gizmos = new List<GizmoData>();

    public void DrawSphere(
        Vector3 position,
        float radius = 0f,
        Variant variant = Variant.PRIMARY,
        Space space = Space.LOCAL,
        bool wire = false
    )
    {
        if (ShouldDraw(variant))
        {
            DrawSphereGizmo(position, variant, radius, space, wire);
        }
    }

    public void AddSphere(
        Transform tform,
        Vector3 position = default(Vector3),
        float radius = 0f,
        Variant variant = Variant.PRIMARY,
        Space space = Space.LOCAL,
        bool wire = false
    )
    {
        if (tform != null)
        {
            position = tform.position;
        }
        else if (position == default(Vector3))
        {
            Debug.LogError("Must provide either a position or a transform");
            return;
        }
        gizmos.Add(
            new GizmoData(
                tform,
                Lists.Of(position),
                variant,
                wire ? Shape.SPHERE_WIRE : Shape.SPHERE,
                radius * Vector3.one,
                space
            )
        );
    }

    public void DrawDirection(
        Vector3 from,
        Vector3 direction,
        bool arrow = false,
        Variant variant = Variant.PRIMARY,
        Space space = Space.LOCAL
    )
    {
        if (ShouldDraw(variant))
        {
            DrawDirectionGizmo(from, direction, variant, space, arrow);
        }
    }

    public void AddDirection(
        Vector3 from,
        Vector3 direction,
        bool arrow = false,
        Variant variant = Variant.PRIMARY,
        Space space = Space.LOCAL
    )
    {
        gizmos.Add(
            new GizmoData(
                null,
                Lists.Of(from, direction),
                variant,
                arrow ? Shape.DIRECTION_ARROW : Shape.DIRECTION,
                direction,
                space
            )
        );
    }

    public void DrawLine(
        Vector3 from,
        Vector3 to,
        Variant variant = Variant.PRIMARY,
        bool arrow = false,
        Space space = Space.LOCAL
    )
    {
        if (ShouldDraw(variant))
        {
            DrawLineGizmo(from, to, variant, space, arrow);
        }
    }

    public void AddLine(
        Vector3 from,
        Vector3 to,
        bool arrow = false,
        Variant variant = Variant.PRIMARY,
        Space space = Space.LOCAL
    )
    {
        gizmos.Add(
            new GizmoData(
                null,
                Lists.Of(from, to),
                variant,
                arrow ? Shape.LINE_ARROW : Shape.LINE,
                Vector3.zero, // size is irrelevant for a line
                space
            )
        );
    }

    private void OnDrawGizmos()
    {
        foreach (var gizmo in gizmos)
        {
            if (ShouldDraw(gizmo.variant))
            {
                DrawGizmo(
                    gizmo.tform,
                    gizmo.positions,
                    gizmo.shape,
                    gizmo.variant,
                    gizmo.size,
                    Vector3.zero,
                    gizmo.space
                );
            }
        }

        if (drawSelf && selfShape != Shape.LINE && selfShape != Shape.LINE_ARROW)
        {
            DrawGizmo(
                transform,
                Lists.Of(transform.position),
                selfShape,
                selfVariant,
                selfSize,
                selfOffset,
                selfSpace
            );
        }
    }

    private void DrawGizmo(
        Transform tform,
        List<Vector3> positions,
        Shape shape,
        Variant variant,
        Vector3 size,
        Vector3 offsetForSelf,
        Space space
    )
    {
        if (size == Vector3.zero)
        {
            size = defaultSize;
        }

        switch (shape)
        {
            case Shape.SPHERE:
            case Shape.SPHERE_WIRE:
                DrawSphereGizmo(
                    positions[0] + offsetForSelf,
                    variant,
                    Vectors.MaxComponent(size) / 2f,
                    space,
                    shape == Shape.SPHERE_WIRE
                );
                break;
            case Shape.CUBE:
            case Shape.CUBE_WIRE:
            case Shape.CUBE_WIRE_CORNERS:
                DrawCubeGizmo(
                    positions[0] + offsetForSelf,
                    variant,
                    size,
                    space,
                    shape == Shape.CUBE_WIRE || shape == Shape.CUBE_WIRE_CORNERS,
                    shape == Shape.CUBE_WIRE_CORNERS
                );
                break;
            case Shape.DIRECTION:
            case Shape.DIRECTION_ARROW:
                DrawDirectionGizmo(
                    positions[0],
                    size,
                    variant,
                    space,
                    shape == Shape.DIRECTION_ARROW
                );
                break;
            case Shape.LINE:
            case Shape.LINE_ARROW:
                DrawLineGizmo(
                    positions[0],
                    positions[1],
                    variant,
                    space,
                    shape == Shape.LINE_ARROW
                );
                break;
        }
    }

    private void DrawSphereGizmo(
        Vector3 position,
        Variant variant,
        float radius,
        Space space,
        bool wire
    )
    {
        if (radius == 0f)
        {
            radius = Vectors.MaxComponent(defaultSize) / 2f;
        }

        Vector3 scale;
        Quaternion rotation;
        GetScaleRotationForSpace(space, position, out scale, out rotation);

        radius *= Vectors.MaxComponent(scale);

        Gizmos.color = GetColor(variant);

        if (wire)
        {
            Gizmos.DrawWireSphere(position, radius);
        }
        else
        {
            Gizmos.DrawSphere(position, radius);
        }
    }

    private void DrawCubeGizmo(
        Vector3 position,
        Variant variant,
        Vector3 size,
        Space space,
        bool wire,
        bool drawCorners
    )
    {
        if (size == Vector3.zero)
        {
            size = defaultSize;
        }

        Vector3 scale;
        Quaternion rotation;
        GetScaleRotationForSpace(space, position, out scale, out rotation);

        size = Vector3.Scale(size, scale);

        Gizmos.color = GetColor(variant);

        if (wire)
        {
            var corners = new List<Vector3>();

            for (int sign1 = -1; sign1 <= 1; sign1 += 2)
            {
                for (int sign2 = -1; sign2 <= 1; sign2 += 2)
                {
                    for (int sign3 = -1; sign3 <= 1; sign3 += 2)
                    {
                        var corner =
                            rotation * Vector3.Scale(size / 2f, new Vector3(sign1, sign2, sign3));
                        corners.Add(corner);
                        if (drawCorners)
                        {
                            Gizmos.DrawSphere(
                                position + corner,
                                sphereCornerSize * Mathf.Min(scale.x, scale.y, scale.z)
                            );
                        }
                    }
                }
            }

            Func<int, int, int, int, int, int, bool> drawLine = delegate(
                int x1,
                int y1,
                int z1,
                int x2,
                int y2,
                int z2
            )
            {
                var v1 = corners[4 * x1 + 2 * y1 + z1];
                var v2 = corners[4 * x2 + 2 * y2 + z2];
                Gizmos.DrawLine(position + v1, position + v2);
                return true;
            };

            for (int i1 = 0; i1 <= 1; i1++)
            {
                for (int i2 = 0; i2 <= 1; i2++)
                {
                    drawLine(i1, i1, i2, 1, 0, i2);
                    drawLine(i1, i1, i2, 0, 1, i2);
                    drawLine(i1, i2, 0, i1, i2, 1);
                }
            }
        }
        else
        {
            // Can't rotate a solid cube because it can't be drawn in terms of lines.
            // Could use a mesh? But that's too much effort.
            // Or a mess of lines? But that wouldn't work with opacity.
            Gizmos.DrawCube(position, size);
        }
    }

    private void DrawDirectionGizmo(
        Vector3 from,
        Vector3 direction,
        Variant variant,
        Space space,
        bool arrow
    )
    {
        Vector3 scale;
        Quaternion rotation;
        GetScaleRotationForSpace(space, from, out scale, out rotation);
        DrawLineGizmo(
            from,
            from + rotation * Vector3.Scale(direction, scale),
            variant,
            space,
            arrow
        );
    }

    private void DrawLineGizmo(Vector3 from, Vector3 to, Variant variant, Space space, bool arrow)
    {
        Gizmos.color = GetColor(variant);
        Gizmos.DrawLine(from, to);

        if (arrow)
        {
            Vector3 scale;
            Quaternion rotation;
            GetScaleRotationForSpace(space, from, out scale, out rotation);

            var arrowheadScaledSize = arrowheadSize * Vectors.MaxComponent(scale);
            var lineVector = to - from;

            // This code is to calculate the "arrowhead vector" - a length and rotation that can
            // be rotated from the line to find the arrowhead point.
            //
            // (Btw it seems to fall apart a bit when the arrowhead length is too large.)
            //
            // Treat the arrowhead length at the hypotenuse of a triangle formed by the end of
            // the line, the arrowhead point, and the intersection point on the line from the
            // arrowhead that forms a right angle. :-)
            //
            // First, to calculate the length of the arrowhead vector, calculate the length of the
            // adjacent side of the triangle, then subtract that from the total line length.
            var adjacentLength = arrowheadScaledSize * Mathf.Cos(arrowheadAngle * Mathf.Deg2Rad);
            var arrowheadVectorLength = lineVector.magnitude - adjacentLength;

            // Next, to calculate the angle of the arrowhead vector, calculate the length of the
            // opposite side of the triangle, then use that with the arrowhead vector length to
            // form a 2nd triangle - giving the arrowhead vector angle as the atan of this.
            var oppositeLength = arrowheadScaledSize * Mathf.Sin(arrowheadAngle * Mathf.Deg2Rad);
            var arrowheadVectorAngle = Mathf.Atan2(oppositeLength, arrowheadVectorLength);

            // Lastly, to calculate the final arrowhead vector, project it along the lineVector
            // then rotate it around an axis which depends on what space it's being drawn in.
            var axis = Vector3.up;

            switch (space)
            {
                case Space.WORLD:
                    // axis = Vector3.up.
                    break;
                case Space.LOCAL:
                    axis = transform.up;
                    break;
                case Space.CAMERA:
                    float camDistance;
                    axis = Cam(out camDistance).transform.forward;
                    break;
            }

            // Calculate the rotation.
            var arrowheadVectorAngleDeg = arrowheadVectorAngle * Mathf.Rad2Deg;
            var leftArrowheadRotation = Quaternion.AngleAxis(arrowheadVectorAngleDeg, axis);
            var rightArrowheadRotation = Quaternion.AngleAxis(-arrowheadVectorAngleDeg, axis);

            // Apply rotation to projection.
            var arrowheadVectorOnLineVector = lineVector.normalized * arrowheadVectorLength;
            var leftArrowheadVector = leftArrowheadRotation * arrowheadVectorOnLineVector;
            var rightArrowheadVector = rightArrowheadRotation * arrowheadVectorOnLineVector;

            Gizmos.DrawLine(to, from + leftArrowheadVector);
            Gizmos.DrawLine(to, from + rightArrowheadVector);
        }
    }

    private Color GetColor(Variant variant)
    {
        switch (variant)
        {
            case Variant.PRIMARY:
                return primary;
            case Variant.SECONDARY:
                return secondary;
            case Variant.TERTIARY:
                return tertiary;
        }
        throw new System.ArgumentException(string.Format("unknown variant: {0}", variant));
    }

    private void GetScaleRotationForSpace(
        Space space,
        Vector3 position,
        out Vector3 scale,
        out Quaternion rotation
    )
    {
        scale = Vector3.one;
        rotation = Quaternion.identity;

        switch (space)
        {
            case Space.WORLD:
                break; // Unity's gizmos are already rendered in world space
            case Space.LOCAL:
                scale = transform.lossyScale;
                rotation = transform.rotation;
                break;
            case Space.CAMERA:
                float cameraDistance;
                var cam = Cam(out cameraDistance);
                scale *= Mathf.Sqrt(
                    cameraDistance * Vector3.Distance(position, cam.transform.position)
                );
                scale *= cameraScale;
                rotation = cam.transform.rotation;
                break;
        }
    }

    private class GizmoData
    {
        public Transform tform;
        public List<Vector3> positions;
        public Variant variant;
        public Shape shape;
        public Vector3 size;
        public Space space;

        public GizmoData(
            Transform tform,
            List<Vector3> positions,
            Variant variant,
            Shape shape,
            Vector3 size,
            Space space
        )
        {
            this.tform = tform;
            this.positions = positions;
            this.variant = variant;
            this.shape = shape;
            this.size = size;
            this.space = space;
        }
    }

    private bool ShouldDraw(Variant variant)
    {
        return enabled
            && (
                (variant == Variant.PRIMARY && drawPrimary)
                || (variant == Variant.SECONDARY && drawSecondary)
                || (variant == Variant.TERTIARY && drawTertiary)
            );
    }

    private void LateUpdate()
    {
        gizmos.Clear();
    }

    private Camera Cam(out float cameraDistance)
    {
#if UNITY_EDITOR
        if (SceneView.currentDrawingSceneView == null)
        {
            // Play mode within the editor.
            cameraDistance = 1f;
            return Camera.main;
        }
        cameraDistance = SceneView.currentDrawingSceneView.cameraDistance;
        return SceneView.currentDrawingSceneView.camera;
#else
        // Gizmos probably aren't enabled anyway, but draw something.
        cameraDistance = 1f;
        return Camera.main;
#endif
    }
}
