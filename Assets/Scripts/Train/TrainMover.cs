using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class TrainMover : MonoBehaviour
{
    [SerializeField]
    private VertexPath path;

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
        SpawnTrain();
        train.transform.position = path.vertices[0];
        trainDistance = 0;
        trainRunning = true;
        trainRunningLeft = false;
        StartCoroutine(MoveCoroutine());
    }

    private void SpawnTrain()
    {
        train = GameObject.Instantiate(path.Net.trainPrefab, transform);
        train.transform.localScale *= path.Net.travelerScale;
        train.transform.position = path.vertices[0];
    }

    private IEnumerator MoveCoroutine()
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

            float totalTrackLength = path.TotalTrackLength();

            if (trainRunningLeft && trainDistance < 0)
            {
                trainRunningLeft = false;
                trainDistance = 0;
            }
            else if (!trainRunningLeft && trainDistance > totalTrackLength)
            {
                trainRunningLeft = true;
                trainDistance = totalTrackLength;
                CompletedTrip();
            }

            MoveTraveler(trainDistance);
            yield return null;
        }
    }

    private void MoveTraveler(float distance)
    {
        float edgeDistance = distance;
        Edge foundEdge = null;
        Vector3 movePos = Vector3.zero;

        Assert.AreNotEqual(path.vertices.Count, 0);
        foreach (var edgeData in path.edges)
        {
            var edge = edgeData.edge;
            if (edgeDistance <= edge.length)
            {
                movePos = Vector3.Lerp(edge.fromVertex, edge.toVertex, edgeDistance / edge.length);
                foundEdge = edge;
                break;
            }
            edgeDistance -= edge.length;
        }

        if (foundEdge == null)
        {
            movePos = path.vertices[path.vertices.Count - 1];
        }

        train.transform.position = movePos;
        train.transform.rotation = Quaternion.LookRotation(foundEdge.extent, Vector3.up);
    }

    private void CompletedTrip()
    {
    }
}
