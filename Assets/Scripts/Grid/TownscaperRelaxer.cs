using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Sylves;

public static class TownscaperRelaxer
{
    public static void Relax(MeshData meshData, int iterations = 10, float forceApplicationPerIteration = 1e-3f)
    {
        // follows Oskar Stalberg's recommended quad relaxing algorithm
        // https://youtu.be/1hqt8JkYRdI?t=1050

        Assert.AreEqual(meshData.indices.Length, 1, "Relaxer does not support submeshes");
        Assert.AreEqual(meshData.topologies[0], Sylves.MeshTopology.Quads, "Relaxer only supports quads");
        var indices = meshData.indices[0];
        Assert.AreEqual(indices.Length % 4, 0, "Topology was quads but indicies did not have a multiple of 4 elements");

        var vertexForceAccumulator = new Vector3[meshData.vertices.Length];

        for (var iterationIndex = 0; iterationIndex < iterations; ++iterationIndex)
        {
            // reset force list values to zero
            for (var vertexIndex = 0; vertexIndex < vertexForceAccumulator.Length; ++vertexIndex)
            {
                vertexForceAccumulator[vertexIndex] = Vector3.zero;
            }

            for (var polyStartIndex = 0; polyStartIndex < indices.Length; polyStartIndex += 4)
            {
                var index0 = indices[polyStartIndex + 0];
                var index1 = indices[polyStartIndex + 0];
                var index2 = indices[polyStartIndex + 0];
                var index3 = indices[polyStartIndex + 0];

                var offsets = CalculateOffsetToFullySquarePoints(
                    meshData.vertices[index0],
                    meshData.vertices[index1],
                    meshData.vertices[index2],
                    meshData.vertices[index3]
                );

                vertexForceAccumulator[index0] += offsets.p0;
                vertexForceAccumulator[index1] += offsets.p1;
                vertexForceAccumulator[index2] += offsets.p2;
                vertexForceAccumulator[index3] += offsets.p3;
            }

            for (var vertexIndex = 0; vertexIndex < meshData.vertices.Length; ++vertexIndex)
            {
                meshData.vertices[vertexIndex] = meshData.vertices[vertexIndex] + vertexForceAccumulator[vertexIndex] * forceApplicationPerIteration;
            }
        }
    }

    private class OffsetToFullySquarePoints
    {
        public Vector3 p0;
        public Vector3 p1;
        public Vector3 p2;
        public Vector3 p3;
    }

    private static OffsetToFullySquarePoints CalculateOffsetToFullySquarePoints(
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3
    )
    {
        var center = Vector3.Lerp(
            Vector3.Lerp(p0, p1, 0.5f),
            Vector3.Lerp(p2, p3, 0.5f),
            0.5f
        );
        var sameSpaceOffset0 = p0 - center;
        var sameSpaceOffset1 = Rotate90_2d(p1 - center);
        var sameSpaceOffset2 = Rotate180_2d(p2 - center);
        var sameSpaceOffset3 = Rotate270_2d(p3 - center);

        var averagedOffset = Vector3.Lerp(
            Vector3.Lerp(sameSpaceOffset0, sameSpaceOffset1, 0.5f),
            Vector3.Lerp(sameSpaceOffset2, sameSpaceOffset3, 0.5f),
            0.5f
        );

        var output = new OffsetToFullySquarePoints();

        output.p0 = averagedOffset;
        output.p1 = Rotate270_2d(averagedOffset);
        output.p2 = Rotate180_2d(averagedOffset);
        output.p3 = Rotate90_2d(averagedOffset);

        return output;
    }

    private static Vector3 Rotate90_2d(Vector3 value)
    {
        return new Vector3(-value.z, value.y, value.x);
    }

    private static Vector3 Rotate180_2d(Vector3 value)
    {
        return new Vector3(-value.x, value.y, -value.z);
    }

    private static Vector3 Rotate270_2d(Vector3 value)
    {
        return new Vector3(value.x, value.y, -value.z);
    }
}
