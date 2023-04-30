using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class TrainMover : MonoBehaviour
{
    [SerializeField]
    private VertexPath path;

    [SerializeField]
    [Min(1)]
    private int smoothSegmentsPerPathSegment = 1;

    private GameObject train;
    private float trainDistance;
    private bool trainRunning;
    private bool trainRunningLeft;

    private void Start()
    {
        path.fullTrackEstablished += OnFullTrackEstablished;
        path.fullTrackBroken += OnFullTrackBroken;
    }

    private void OnFullTrackEstablished()
    {
        StartMoving();
    }

    private void OnFullTrackBroken()
    {
        GameObject.Destroy(train);
        trainRunning = false;
    }

    public void StartMoving()
    {
        var smoothPath = CalculateSmoothPath();
        SpawnTrain();
        train.transform.position = path.vertices[0];
        trainDistance = 0;
        trainRunning = true;
        trainRunningLeft = false;
        StartCoroutine(MoveCoroutine(smoothPath));
    }

    private class SmoothPath
    {
        public SmoothPathSegment[] path;
        public float totalDistance;
    }

    private struct SmoothPathSegment
    {
        public Vector3 startPosition;
        public float length; // 0 for end
    }

    private SmoothPath CalculateSmoothPath()
    {
        var result = new SmoothPathSegment[(path.edges.Count - 1) * smoothSegmentsPerPathSegment + 1];

        var cumulativeDistance = 0f;

        for (var pathSegmentIndex = 0; pathSegmentIndex < path.edges.Count - 1; ++pathSegmentIndex)
        {
            SetSmoothPathSubArray(
                result,
                pathSegmentIndex * smoothSegmentsPerPathSegment,
                smoothSegmentsPerPathSegment,
                path.edges[pathSegmentIndex].edge.middle,
                path.edges[pathSegmentIndex].edge.toVertex,
                path.edges[pathSegmentIndex + 1].edge.middle,
                ref cumulativeDistance
            );
        }

        result[path.edges.Count - 1 * smoothSegmentsPerPathSegment] = new SmoothPathSegment{
            startPosition = path.edges[0].edge.middle,
            length = 0f,
        };

        return new SmoothPath{
            path = result,
            totalDistance = cumulativeDistance,
        };
    }

    private static void SetSmoothPathSubArray(SmoothPathSegment[] outputPaths, int startIndex, int segments, Vector3 start, Vector3 control, Vector3 end,
        ref float cumulativeDistance
    )
    {
        for (var smoothPathInnerIndex = 0; smoothPathInnerIndex < segments; ++smoothPathInnerIndex)
        {
            var outputIndex = startIndex + smoothPathInnerIndex;

            var inSegmentStartProportion = (float)smoothPathInnerIndex / segments;
            var inSegmentEndProportion = (float)(smoothPathInnerIndex + 1) / segments;

            var segmentStartPosition = Bezier.Calculate(start, control, end, inSegmentStartProportion).point;
            var segmentEndPosition = Bezier.Calculate(start, control, end, inSegmentEndProportion).point;

            var distance = (segmentEndPosition - segmentStartPosition).magnitude;

            outputPaths[outputIndex] = new SmoothPathSegment{
                startPosition = segmentStartPosition,
                length = distance,
            };

            cumulativeDistance += distance;
        }
    }

    private void SpawnTrain()
    {
        train = GameObject.Instantiate(path.Net.trainPrefab, transform);
        train.transform.localScale *= path.Net.travelerScale;
        train.transform.position = path.vertices[0];
    }

    private IEnumerator MoveCoroutine(SmoothPath smoothPath)
    {
        while (trainRunning)
        {
            if (trainRunningLeft)
            {
                trainDistance -= path.Net.moveSpeed * Time.deltaTime;
            }
            else
            {
                trainDistance += path.Net.moveSpeed * Time.deltaTime;
            }

            if (trainRunningLeft && trainDistance < 0)
            {
                trainRunningLeft = false;
                trainDistance = 0;
            }
            else if (!trainRunningLeft && trainDistance > smoothPath.totalDistance)
            {
                trainRunningLeft = true;
                trainDistance = smoothPath.totalDistance;
                CompletedTrip();
            }

            MoveTraveler(trainDistance, smoothPath);
            yield return null;
        }
    }

    private void MoveTraveler(float distance, SmoothPath path)
    {
        float remainingDistance = distance;

        Assert.AreNotEqual(path.path.Length, 0);
        for (var segmentIndex = 0; segmentIndex < path.path.Length; ++segmentIndex)
        {
            var segment = path.path[segmentIndex];
            if (remainingDistance <= segment.length)
            {
                train.transform.position = Vector3.Lerp(segment.startPosition, path.path[segmentIndex + 1].startPosition, remainingDistance / segment.length);
                // train.transform.rotation = Quaternion.LookRotation(foundEdge.extent, Vector3.up);
                return;
            }
            remainingDistance -= segment.length;
        }

        Assert.IsTrue(remainingDistance > 0f, "If edge distance is less than 0 it should have chosen one of the path segments");

        var finalSegment = path.path[path.path.Length - 1];
        train.transform.position = finalSegment.startPosition;
        // train.transform.rotation = Quaternion.LookRotation(foundEdge.extent, Vector3.up);
    }

    private void CompletedTrip()
    {
    }
}
