using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrackModelGenerator
{
    public Mesh GenerateTracksBetween(Vector3 start, Vector3 control, Vector3 end, Mesh templateMesh, int segments)
    {
        var compositeMesh = new Mesh();

        // assume template mesh travels from 0 to +1 z
        // assume template mesh centered on x

        var sourceVertices = templateMesh.vertices;
        var sourceNormals = templateMesh.normals;

        var outputVertices = new List<Vector3>();
        var outputNormals = new List<Vector3>();

        for (var segmentIndex = 0; segmentIndex < segments; ++segmentIndex)
        {
            var startBezierT = (float)segmentIndex / segments;
            var startBezierValues = CalculateBezier(start, control, end, startBezierT);

            var endBezierT = (float)(segmentIndex + 1) / segments;
            var endBezierValues = CalculateBezier(start, control, end, endBezierT);

            // create a cage warp from the segment start bezier point to the segment end bezier point
            var v0 = startBezierValues.point - startBezierValues.crossZ * 0.5f;
            var v1 = startBezierValues.point + startBezierValues.crossZ * 0.5f;
            var v2 = endBezierValues.point - endBezierValues.crossZ * 0.5f;
            var v3 = endBezierValues.point + endBezierValues.crossZ * 0.5f;
            var warper = CageWarper.FromVerticesWithTrackNormals(v0, v1, v2, v3,
                startBezierValues.crossZ, endBezierValues.crossZ,
                startBezierValues.tangent, endBezierValues.tangent
            );

            outputVertices.AddRange(sourceVertices.Select(vertex => warper.WarpVertex(vertex)));
            outputNormals.AddRange(Enumerable.Zip(sourceVertices, sourceNormals, (vertex, normal) => warper.WarpNormal(vertex, normal)));
        }

        compositeMesh.subMeshCount = templateMesh.subMeshCount;
        var templateVerticesCount = sourceVertices.Length;
        for (var submeshIndex = 0; submeshIndex < templateMesh.subMeshCount; ++submeshIndex)
        {
            var templateIndices = templateMesh.GetIndices(submeshIndex);
            var indices = new List<int>();
            for (var segmentIndex = 0; segmentIndex < segments; ++segmentIndex)
            {
                indices.AddRange(templateIndices.Select(index => index + segmentIndex * templateVerticesCount));
            }
            compositeMesh.SetIndices(indices, templateMesh.GetTopology(submeshIndex), submeshIndex);
        }

        return compositeMesh;
    }

    private struct BezierResults
    {
        public Vector3 point;
        public Vector3 tangent;
        public Vector3 crossZ; // normal when viewed from above
    }

    private static BezierResults CalculateBezier(Vector3 start, Vector3 control, Vector3 end, float t)
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
