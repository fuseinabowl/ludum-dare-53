using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [SerializeField]
    private TrainAudio trainAudio;

    private bool trainRunningLeft;
    private GameObject headLocomotive;
    private GameObject tailLocomotive;
    private List<GameObject> carriages = new List<GameObject>();
    private float trainDistance;
    private bool trainRunning;

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
        GameObject.Destroy(headLocomotive);
        GameObject.Destroy(tailLocomotive);
        foreach (var carriage in carriages)
        {
            GameObject.Destroy(carriage);
        }
        carriages.Clear();
        SetTrainRunning(false);
    }

    public void StartMoving()
    {
        var smoothPath = CalculateSmoothPath();
        SpawnTrain();
        headLocomotive.transform.position = path.vertices[0];
        trainDistance = 0;
        SetTrainRunning(true);
        StartCoroutine(MoveCoroutine(smoothPath));
    }

    private void SetTrainRunning(bool running)
    {
        trainRunning = running;
        var camera = SingletonProvider.Get<FreeFollowCamera>();
        if (running)
        {
            trainRunningLeft = false;
            camera.AddFollow(gameObject, CameraPose());
            trainAudio.chug.Play();
        }
        else
        {
            camera.RemoveFollow(gameObject);
            trainAudio.chug.Stop();
        }
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
        var result = new SmoothPathSegment[
            (path.edges.Count - 1) * smoothSegmentsPerPathSegment + 2
        ];

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
        result[(path.edges.Count - 1) * smoothSegmentsPerPathSegment] = new SmoothPathSegment
        {
            startPosition = finalEdge.middle,
            facingDirection = (finalEdge.toVertex - finalEdge.middle).normalized,
            length = Vector3.Distance(finalEdge.middle, finalEdge.toVertex),
        };

        cumulativeDistance += Vector3.Distance(finalEdge.middle, finalEdge.toVertex);

        result[(path.edges.Count - 1) * smoothSegmentsPerPathSegment + 1] = new SmoothPathSegment
        {
            startPosition = finalEdge.toVertex,
            facingDirection = (finalEdge.toVertex - finalEdge.middle).normalized,
            length = 0f,
        };

        return new SmoothPath { path = result, totalDistance = cumulativeDistance, };
    }

    private static void SetSmoothPathSubArray(
        SmoothPathSegment[] outputPaths,
        int startIndex,
        int segments,
        Vector3 start,
        Vector3 control,
        Vector3 end,
        ref float cumulativeDistance
    )
    {
        for (var smoothPathInnerIndex = 0; smoothPathInnerIndex < segments; ++smoothPathInnerIndex)
        {
            var outputIndex = startIndex + smoothPathInnerIndex;

            var inSegmentStartProportion = (float)smoothPathInnerIndex / segments;
            var inSegmentEndProportion = (float)(smoothPathInnerIndex + 1) / segments;

            var segmentStartPosition = Bezier
                .Calculate(start, control, end, inSegmentStartProportion)
                .point;
            var segmentEndPosition = Bezier
                .Calculate(start, control, end, inSegmentEndProportion)
                .point;

            var distance = (segmentEndPosition - segmentStartPosition).magnitude;

            outputPaths[outputIndex] = new SmoothPathSegment
            {
                startPosition = segmentStartPosition,
                facingDirection = (segmentEndPosition - segmentStartPosition).normalized, // not accurate, should be from the previous start to this end
                length = distance,
            };

            cumulativeDistance += distance;
        }
    }

    private void SpawnTrain()
    {
        headLocomotive = GameObject.Instantiate(trainData.locomotive, transform);
        headLocomotive.transform.position = path.vertices[0];

        for (var carriageIndex = 0; carriageIndex < trainData.numberOfCarriages; ++carriageIndex)
        {
            carriages.Add(GameObject.Instantiate(trainData.carriage, transform));
        }

        tailLocomotive = GameObject.Instantiate(trainData.locomotive, transform);
        tailLocomotive.transform.position = path.vertices[0];
        tailLocomotive.transform.localScale = new Vector3(-1f, 1f, -1f);
    }

    private float TrainLength => trainData.carriageLength * (trainData.numberOfCarriages + 2);

    private IEnumerator MoveCoroutine(SmoothPath smoothPath)
    {
        PlaceTrainCarsAtStartOfPath(smoothPath);
        yield return LoadCarriagesAndShowHeadLocomotive();

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

                PlaceTrainCarsAtStartOfPath(smoothPath);

                yield return LoadCarriagesAndShowHeadLocomotive();
            }
            else if (!trainRunningLeft && trainDistance + TrainLength > smoothPath.totalDistance)
            {
                trainRunningLeft = true;
                trainDistance = smoothPath.totalDistance - TrainLength;

                PlaceTrainCarsAtEndOfPath(smoothPath);

                yield return UnloadCarriagesAndShowTailLocomotive();
            }
            else
            {
                PlaceTrainCarsAlongPath(smoothPath, trainDistance);
            }

            yield return null;
        }
    }

    private void PlaceTrainCarsAtStartOfPath(SmoothPath smoothPath)
    {
        PlaceTrainCarsAlongPath(smoothPath, 0f);
    }

    private void PlaceTrainCarsAtEndOfPath(SmoothPath smoothPath)
    {
        var trainPositionForEndOfPath = smoothPath.totalDistance - TrainLength;
        PlaceTrainCarsAlongPath(smoothPath, trainPositionForEndOfPath);
    }

    private void PlaceTrainCarsAlongPath(SmoothPath smoothPath, float tipDistanceAlongPath)
    {
        var distanceAlongPath = tipDistanceAlongPath + 0.5f * trainData.carriageLength;
        PlaceTrainCar(tailLocomotive, distanceAlongPath, smoothPath);
        for (var carriageIndex = 0; carriageIndex < carriages.Count; ++carriageIndex)
        {
            var carriage = carriages[carriageIndex];
            PlaceTrainCar(
                carriage,
                distanceAlongPath + trainData.carriageLength * (carriageIndex + 1),
                smoothPath
            );
        }
        PlaceTrainCar(
            headLocomotive,
            distanceAlongPath + trainData.carriageLength * (carriages.Count + 1),
            smoothPath
        );
    }

    private IEnumerator LoadCarriagesAndShowHeadLocomotive()
    {
        trainAudio.pauseChug();

        HideTailLocomotive();

        yield return new WaitForSeconds(trainData.timeBetweenHidingLocomotiveAndLoadingCarriages);

        trainAudio.loaded.Play();

        foreach (var carriage in carriages)
        {
            LoadCarriage(carriage);
            yield return new WaitForSeconds(trainData.timeBetweenLoadingCarriages);
        }

        ShowHeadLocomotive();

        yield return new WaitForSeconds(trainData.timeBetweenShowingLocomotiveAndStartingMoving);

        trainAudio.resumeChug();
    }

    private IEnumerator UnloadCarriagesAndShowTailLocomotive()
    {
        trainAudio.pauseChug();

        HideHeadLocomotive();

        yield return new WaitForSeconds(trainData.timeBetweenHidingLocomotiveAndLoadingCarriages);

        trainAudio.unloaded.Play();

        foreach (var carriage in Enumerable.Reverse(carriages))
        {
            UnloadCarriage(carriage);
            yield return new WaitForSeconds(trainData.timeBetweenLoadingCarriages);
        }

        ShowTailLocomotive();

        yield return new WaitForSeconds(trainData.timeBetweenShowingLocomotiveAndStartingMoving);

        trainAudio.resumeChug();
    }

    private void HideTailLocomotive()
    {
        SetLocomotiveShown(tailLocomotive, false);
    }

    private void HideHeadLocomotive()
    {
        SetLocomotiveShown(headLocomotive, false);
    }

    private void ShowTailLocomotive()
    {
        SetLocomotiveShown(tailLocomotive, true);
    }

    private void ShowHeadLocomotive()
    {
        SetLocomotiveShown(headLocomotive, true);
    }

    private void SetLocomotiveShown(GameObject locomotive, bool shouldBeShown)
    {
        var animator = locomotive.GetComponent<Animator>();
        animator.SetBool(trainData.locomotiveAnimatorShownName, shouldBeShown);
    }

    private void LoadCarriage(GameObject carriage)
    {
        SetCarriageFilled(carriage, true);
    }

    private void UnloadCarriage(GameObject carriage)
    {
        SetCarriageFilled(carriage, false);
    }

    private void SetCarriageFilled(GameObject carriage, bool shouldBeFilled)
    {
        var animator = carriage.GetComponent<Animator>();
        animator.SetBool(trainData.carriageAnimatorFilledName, shouldBeFilled);
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
                trainCar.transform.position = Vector3.Lerp(
                    segment.startPosition,
                    nextSegment.startPosition,
                    proportionThroughThisSegment
                );
                var facingDirection = Vector3.Lerp(
                    segment.facingDirection,
                    nextSegment.facingDirection,
                    proportionThroughThisSegment
                );
                trainCar.transform.rotation = Quaternion.LookRotation(facingDirection, Vector3.up);
                return;
            }
            remainingDistance -= segment.length;
        }

        Assert.IsTrue(
            remainingDistance > 0f,
            "If edge distance is less than 0 it should have chosen one of the path segments"
        );

        var finalSegment = path.path[path.path.Length - 1];
        trainCar.transform.position = finalSegment.startPosition;
        trainCar.transform.rotation = Quaternion.LookRotation(
            finalSegment.facingDirection,
            Vector3.up
        );
    }

    public bool Running()
    {
        return trainRunning;
    }

    private Pose CameraPose()
    {
        if (trainRunningLeft) {
            return Poses.Reverse(Poses.FromTransform(tailLocomotive.transform));
        }
        return Poses.FromTransform(headLocomotive.transform);
    }

    private void Update()
    {
        if (trainRunning)
        {
            var pose = CameraPose();
            SingletonProvider.Get<FreeFollowCamera>().SetFollowPose(gameObject, pose);
            trainAudio.transform.position = pose.position;
        }
    }
}
