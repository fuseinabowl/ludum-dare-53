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

    [SerializeField]
    private TrainData trainData;

    private GameObject locomotive;
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
        GameObject.Destroy(locomotive);
        trainRunning = false;
    }

    public void StartMoving()
    {
        var smoothPath = CalculateSmoothPath();
        SpawnTrain();
        locomotive.transform.position = path.vertices[0];
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
        public Vector3 facingDirection;
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

        var finalEdge = path.edges[path.edges.Count - 1].edge;
        result[(path.edges.Count - 1) * smoothSegmentsPerPathSegment] = new SmoothPathSegment{
            startPosition = finalEdge.middle,
            facingDirection = (finalEdge.toVertex - finalEdge.middle).normalized,
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
                facingDirection = (segmentEndPosition - segmentStartPosition).normalized, // not accurate, should be from the previous start to this end
                length = distance,
            };

            cumulativeDistance += distance;
        }
    }

    private void SpawnTrain()
    {
        locomotive = GameObject.Instantiate(trainData.locomotive, transform);
        locomotive.transform.position = path.vertices[0];
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

            PlaceTrainCar(locomotive, trainDistance, smoothPath);
            yield return null;
        }
    }

    private void PlaceTrainCar(GameObject trainCar, float distance, SmoothPath path)
    {
        float remainingDistance = distance;

        Assert.AreNotEqual(path.path.Length, 0);
        for (var segmentIndex = 0; segmentIndex < path.path.Length - 1; ++segmentIndex)
        {
            var segment = path.path[segmentIndex];
            var nextSegment = path.path[segmentIndex + 1];
            if (remainingDistance <= segment.length)
            {
                var proportionThroughThisSegment = remainingDistance / segment.length;
                trainCar.transform.position = Vector3.Lerp(segment.startPosition, nextSegment.startPosition, proportionThroughThisSegment);
                var facingDirection = Vector3.Lerp(segment.facingDirection, nextSegment.facingDirection, proportionThroughThisSegment);
                trainCar.transform.rotation = Quaternion.LookRotation(facingDirection, Vector3.up);
                return;
            }
            remainingDistance -= segment.length;
        }

        Assert.IsTrue(remainingDistance > 0f, "If edge distance is less than 0 it should have chosen one of the path segments");

        var finalSegment = path.path[path.path.Length - 1];
        trainCar.transform.position = finalSegment.startPosition;
        trainCar.transform.rotation = Quaternion.LookRotation(finalSegment.facingDirection, Vector3.up);
    }

    private void CompletedTrip()
    {
    }
}
